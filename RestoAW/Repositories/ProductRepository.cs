using Microsoft.Data.SqlClient;
using RestoAW.Models;
using RestoAW.Repositories.IRepositories;
using System.Transactions;

namespace RestoAW.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT * FROM Table_Products";
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        products.Add(new Product
                        {
                            ProductId = (int)reader["productId"],
                            Name = reader["name"].ToString(),
                            Description = reader["description"].ToString(),
                            Price = (decimal)reader["price"],
                            ImageURL = reader["imageURL"].ToString(),
                            Stock = (int)reader["stock"],
                            CreatedAt = (DateTime)reader["createdAt"]
                        });
                    }
                }
            }

            return products;
        }
        public async Task InsertFullTransactionAsync(BuyerDto buyer, Dictionary<string, CartItemDto> cartItems, string status)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();

            try
            {
                // 1. Insert User
                int userId;
                string insertUser = @"INSERT INTO Table_Users (name, phone, email, address) 
                                  OUTPUT INSERTED.userId 
                                  VALUES (@name, @phone, @email, @address)";

                using (var cmd = new SqlCommand(insertUser, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@name", buyer.Name);
                    cmd.Parameters.AddWithValue("@phone", buyer.Phone);
                    cmd.Parameters.AddWithValue("@email", buyer.Email);
                    cmd.Parameters.AddWithValue("@address", buyer.Address);
                    userId = (int)await cmd.ExecuteScalarAsync();
                }

                // 2. Insert CartItems
                string lastThreeDigits = buyer.Phone[^3..];
                string datePart = DateTime.Now.ToString("MMdd");
                string timePart = DateTime.Now.ToString("HHmmss");
                string cartCode = $"{lastThreeDigits}_{datePart}_{timePart}";

                int cartId;
                string insertCart = @"INSERT INTO Table_CartItems (userId, cart_code) 
                                  OUTPUT INSERTED.cartId 
                                  VALUES (@userId, @cartCode)";

                using (var cmd = new SqlCommand(insertCart, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@cartCode", cartCode);
                    cartId = (int)await cmd.ExecuteScalarAsync();
                }

                decimal totalPrice = 0;

                // 3. Insert CartProducts
                foreach (var item in cartItems.Values)
                {
                    decimal subtotal = item.Price * item.Quantity;
                    totalPrice += subtotal;

                    string insertCartProduct = @"INSERT INTO Table_Carts_Products 
                        (cartId, productId, totalPaidItem, quantity, createdAt, noteCustomer)
                        VALUES (@cartId, @productId, @total, @qty, @createdAt, @note)";

                    using var cmd = new SqlCommand(insertCartProduct, conn, tran);
                    cmd.Parameters.AddWithValue("@cartId", cartId);
                    cmd.Parameters.AddWithValue("@productId", item.ProductId);
                    cmd.Parameters.AddWithValue("@total", subtotal);
                    cmd.Parameters.AddWithValue("@qty", item.Quantity);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@note", item.Note ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4. Insert Transaction
                int transId;
                string insertTrans = @"INSERT INTO Table_Transactions 
                        (cartId, totalPrice, status, paymentTime, createdAt) 
                        OUTPUT INSERTED.transId
                        VALUES (@cartId, @total, @status, @payTime, @createdAt)";

                using (var cmd = new SqlCommand(insertTrans, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@cartId", cartId);
                    cmd.Parameters.AddWithValue("@total", totalPrice);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@payTime", DBNull.Value); // null for now
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                    transId = (int)await cmd.ExecuteScalarAsync();
                }

                // 5. Insert PaymentNotif dummy
                string insertNotif = @"INSERT INTO Table_PaymentNotif 
                        (transId, status, RawJson, ReceivedAt) 
                        VALUES (@transId, @status, @raw, @receivedAt)";

                using (var cmd = new SqlCommand(insertNotif, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@transId", transId);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@raw", "{\"midtrans\":\"dummy_json\"}");
                    cmd.Parameters.AddWithValue("@receivedAt", DateTime.Now);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }
        public async Task<List<TransactionDTO>> GetTransactionsByEmailAsync(string email)
        {
            var transactions = new List<TransactionDTO>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = @"
                SELECT t.*, c.cart_code
                FROM Table_Transactions t
                JOIN Table_CartItems c ON t.cartId = c.cartId
                JOIN Table_Users u ON c.userId = u.userId
                WHERE u.email = @Email
                ORDER BY t.createdAt DESC;";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            transactions.Add(new TransactionDTO
                            {
                                TransId = (int)reader["transId"],
                                CartId = (int)reader["cartId"],
                                CartCode = reader["cart_code"].ToString(),
                                TotalPrice = (decimal)reader["totalPrice"],
                                Status = reader["status"].ToString(),
                                PaymentTime = reader["paymentTime"] == DBNull.Value ? null : (DateTime?)reader["paymentTime"],
                                CreatedAt = (DateTime)reader["createdAt"]
                            });
                        }
                    }
                }
            }

            return transactions;
        }
        public async Task<bool> UpdateTransactionStatusAsync(string orderId, string status, string rawJson, DateTime receivedAt)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        string getTransIdSql = @"
                            SELECT t.transId
                            FROM Table_Transactions t
                            JOIN Table_CartItems c ON t.cartId = c.cartId
                            WHERE c.cart_code = @OrderId";

                        int transId;
                        using (var cmd = new SqlCommand(getTransIdSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", orderId);
                            transId = (int?)await cmd.ExecuteScalarAsync() ?? 0;
                            if (transId == 0) throw new Exception("Transaction not found");
                        }

                        string updateTransSql = @"
                            UPDATE Table_Transactions
                            SET status = @Status,
                                paymentTime = CASE 
                                    WHEN @Status IN ('settlement', 'capture', 'deny', 'cancel', 'expire') 
                                    THEN @ReceivedAt
                                    ELSE NULL
                                END
                            WHERE transId = @TransId";

                        using (var cmd = new SqlCommand(updateTransSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Status", status);
                            cmd.Parameters.AddWithValue("@ReceivedAt", receivedAt);
                            cmd.Parameters.AddWithValue("@TransId", transId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        string updateNotifSql = @"
                            UPDATE Table_PaymentNotif
                            SET status = @Status,
                                RawJson = @RawJson,
                                ReceivedAt = @ReceivedAt
                            WHERE transId = @TransId";

                        using (var cmd = new SqlCommand(updateNotifSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Status", status);
                            cmd.Parameters.AddWithValue("@RawJson", rawJson);
                            cmd.Parameters.AddWithValue("@ReceivedAt", receivedAt);
                            cmd.Parameters.AddWithValue("@TransId", transId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        await tran.CommitAsync();
                        return true;
                    }
                    catch
                    {
                        await tran.RollbackAsync();
                        return false;
                    }
                }
            }
        }
    }


}
