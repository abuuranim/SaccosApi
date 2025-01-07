using System.ComponentModel.DataAnnotations;

namespace SaccosApi.Models
{
    public class LoanType
    {
        public int LoanTypeID { get; set; }

        [Required(ErrorMessage = "Loan Name is required")]
        public string? LoanName { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Loan interest is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Loan interest must be greater than zero.")]
        public double LoanInterest { get; set; }

        [Required(ErrorMessage = "Interest calculation method is required")]
        //[Range(1, int.MaxValue, ErrorMessage = "Loan duration must be greater than zero.")]
        public int InterestCalculationMethodID { get; set; }
        public string? MethodName { get; set; }
        public double ProcessingFee { get; set; }
        public double LoanInsuranceFee { get; set; }
        public double LoanPenalty { get; set; }
        public double MinimumLimit { get; set; }
        public double MaximumLimit { get; set; }
        public int RequireGuarranters { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
