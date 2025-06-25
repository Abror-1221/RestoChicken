namespace RestoAW.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ImageURL { get; set; }
        public int Stock { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BuyerDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
    }


    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public string Image { get; set; }
        public string Note { get; set; }
    }

    public class TransactionDTO
    {
        public int TransId { get; set; }
        public int CartId { get; set; }
        public string CartCode { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public DateTime? PaymentTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MidtransOrderDTO
    {
        public string OrderId { get; set; }
        public decimal Total { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    public class CheckoutPayload
    {
        public BuyerDto Buyer { get; set; }
        public Dictionary<string, CartItemDto> CartItems { get; set; }
        public string Status { get; set; }
    }
}
