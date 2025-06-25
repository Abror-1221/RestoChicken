using Microsoft.Extensions.Options;
using RestoAW.Models;
using RestoAW.Repositories.IRepositories;
using RestoAW.Services.IServices;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Transactions;

namespace RestoAW.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly MidtransSettings _midtransSettings;

        public ProductService(IProductRepository productRepository, IOptions<MidtransSettings> midtransOptions)
        {
            _productRepository = productRepository;
            _midtransSettings = midtransOptions.Value;
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            return await _productRepository.GetAllProductsAsync();
        }
        public async Task<bool> InsertFullTransactionAsync(BuyerDto buyer, Dictionary<string, CartItemDto> cartItems, string status)
        {
            try
            {
                await _productRepository.InsertFullTransactionAsync(buyer, cartItems, status);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public Task<List<TransactionDTO>> GetTransactionsByEmailAsync(string email)
        {
            return _productRepository.GetTransactionsByEmailAsync(email);
        }
        public async Task<string> GetSnapTokenAsync(MidtransTransaction request)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(_midtransSettings.ServerKey + ":")));

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://app.sandbox.midtrans.com/snap/v1/transactions", content);
            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);
            return doc.RootElement.GetProperty("token").GetString();
        }
        public async Task<bool> UpdateTransactionFromWebhookAsync(string orderId, string status, string rawJson, DateTime receivedAt)
        {
            return await _productRepository.UpdateTransactionStatusAsync(orderId, status, rawJson, receivedAt);
        }

    }

}
