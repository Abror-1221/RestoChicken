namespace RestoAW.Models
{
    public class MidtransTransaction
    {
        public TransactionDetails transaction_details { get; set; }
        public CustomerDetails customer_details { get; set; }
    }

    public class TransactionDetails
    {
        public string order_id { get; set; }
        public decimal gross_amount { get; set; }
    }

    public class CustomerDetails
    {
        public string first_name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
    }

    public class MidtransWebhookDTO
    {
        public string transaction_status { get; set; }
        public string order_id { get; set; }
    }

    public class GroupData
    {
        public string OrderId { get; set; }         
        public string Status { get; set; }          
        public string RawJson { get; set; }         
        public DateTime ReceivedAt { get; set; }    
    }


    public class MidtransSettings
    {
        public string ServerKey { get; set; }
    }
}
