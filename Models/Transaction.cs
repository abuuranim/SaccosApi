using System.ComponentModel.DataAnnotations;

namespace SaccosApi.Models
{
    public class Transaction
    {
        public Guid TransactionID { get; set; }
        public Guid AccountID { get; set; }

        [Required(ErrorMessage = "Transaction amount is required")]
        [Range(100, Int64.MaxValue, ErrorMessage = "amount must be greater than 100.")]
        public double TransactionAmount { get; set; }
        public string? Description { get; set; }
        public int PaymentTypeID { get; set; }
        public string? PaymentMethod { get; set; }
        public int AccountTypeID { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? TransactionType { get; set; }
        public string? VourcherNumber { get; set; }



    }
}


