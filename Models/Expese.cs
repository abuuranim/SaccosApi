using System.ComponentModel.DataAnnotations;

namespace SaccosApi.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item Description is required")]
        public string? ItemDescription { get; set; }


        public string? Vendor { get; set; }

        [Required(ErrorMessage = "Cost is required")]
        [Range(100, Int32.MaxValue, ErrorMessage = "cost must be greater than 100.")]
        public double Cost { get; set; }


        [Required(ErrorMessage = "Payment date is required")]
        public DateTime PaymentDate { get; set; }

        [Required(ErrorMessage = "Payment Reference No. is required")]
        public string? ReferenceNo { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }

        public string? LastModifiedBy { get; set; }
        public DateTime LastModified { get; set; }
    }
}
