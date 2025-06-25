using Microsoft.AspNetCore.Mvc;
using RestoAW.Models;
using RestoAW.Services.IServices;
using System.Text.Json;

namespace RestoAW.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetProductsAsync();
            return View(products);
        }

        public IActionResult CheckOut()
        {
            return View();
        }

        public async Task<IActionResult> Transactions(string email, string status = null)
        {
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Index", "Home");

            var transactions = await _productService.GetTransactionsByEmailAsync(email);

            ViewBag.Status = status;
            return View(transactions);
        }


        [HttpPost]
        public async Task<IActionResult> TransactionsPay([FromBody] CheckoutPayload payload)
        {
            payload.Status ??= "On process";

            bool success = await _productService.InsertFullTransactionAsync(payload.Buyer, payload.CartItems, payload.Status);

            return Json(new { status = success ? "success" : "failed" });
        }

        [HttpPost("/api/payment/update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] GroupData data)
        {
            bool result = await _productService.UpdateTransactionFromWebhookAsync(
                data.OrderId, data.Status, data.RawJson, data.ReceivedAt);

            return Ok(new { status = result ? "updated" : "failed" });
        }

        [HttpPost("/api/payment/token")]
        public async Task<IActionResult> GetSnapToken([FromBody] CheckoutPayload payload)
        {
            var response = await _productService.GetTransactionsByEmailAsync(payload.Buyer.Email);

            var snapRequest = new MidtransTransaction
            {
                transaction_details = new TransactionDetails
                {
                    order_id = response.FirstOrDefault().CartCode,
                    gross_amount = response.FirstOrDefault().TotalPrice
                },
                customer_details = new CustomerDetails
                {
                    first_name = payload.Buyer.Name,
                    email = payload.Buyer.Email,
                    phone = payload.Buyer.Phone
                }
            };

            var snapToken = await _productService.GetSnapTokenAsync(snapRequest);
            return Json(new { token = snapToken });
        }

        [HttpPost("/api/payment/notification")]
        public async Task<IActionResult> ReceiveNotification()
        {
            string raw = await new StreamReader(Request.Body).ReadToEndAsync();
            var notif = JsonSerializer.Deserialize<MidtransWebhookDTO>(raw);

            string status = notif.transaction_status;
            string orderId = notif.order_id;
            DateTime receivedAt = DateTime.Now;

            bool result = await _productService.UpdateTransactionFromWebhookAsync(orderId, status, raw, receivedAt);

            return result ? Ok(new { message = "Notification handled." }) : StatusCode(500, "Failed to update.");
        }
    }
}
