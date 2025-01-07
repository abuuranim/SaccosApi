using System.ComponentModel.DataAnnotations;

namespace SaccosApi.Models
{
    public class InvoiceDetails
    {
        public Int64 InvoiceDetailID { get; set; }
        public string? InvoiceNo { get; set; }
        public double Amount { get; set; }
        public string? Description { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
