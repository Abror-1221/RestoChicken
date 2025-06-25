using RestoAW.Models;
using System.Transactions;

namespace RestoAW.Services.IServices
{
    public interface IProductService
    {
        Task<List<Product>> GetProductsAsync();
        Task<bool> InsertFullTransactionAsync(BuyerDto buyer, Dictionary<string, CartItemDto> cartItems, string status);
        Task<List<TransactionDTO>> GetTransactionsByEmailAsync(string email);
        Task<string> GetSnapTokenAsync(MidtransTransaction request);
        Task<bool> UpdateTransactionFromWebhookAsync(string orderId, string status, string rawJson, DateTime receivedAt);
    }
}
