using System.ComponentModel.DataAnnotations;

namespace SaccosApi.Models
{
    public class Invoice
    {

        public Invoice() {

            this.InvoiceDetails = new HashSet<InvoiceDetails>();
        }
        public Int64 InvoiceID { get; set; }
        public string? InvoiceNo { get; set; }

        public string? PaymentKey { get; set; }

        [Required(ErrorMessage = "Invoice Amount is required")]
        [Range(100, Int32.MaxValue, ErrorMessage = "Invoice Amount must be greater than 100.")]
        public double InvoiceAmount { get; set; }
        public DateTime InvoiceDate { get; set; }

        public string? InvoiceDescription { get; set; }
        public string? ControlNo { get; set; }

        public ICollection<InvoiceDetails>? InvoiceDetails { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
