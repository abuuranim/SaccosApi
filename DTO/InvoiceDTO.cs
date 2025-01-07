namespace SaccosApi.DTO
{
    public class InvoiceDTO
    {
        public string? InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }
}

