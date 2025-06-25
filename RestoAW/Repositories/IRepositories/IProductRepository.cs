using RestoAW.Models;
using System.Transactions;

namespace RestoAW.Repositories.IRepositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllProductsAsync();
        Task InsertFullTransactionAsync(BuyerDto buyer, Dictionary<string, CartItemDto> cartItems, string status);
        Task<List<TransactionDTO>> GetTransactionsByEmailAsync(string email);
        Task<bool> UpdateTransactionStatusAsync(string orderId, string status, string rawJson, DateTime receivedAt);
    }
}
