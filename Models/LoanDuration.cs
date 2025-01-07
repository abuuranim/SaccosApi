using System.ComponentModel.DataAnnotations;

namespace SaccosApi.Models
{
    public class LoanDuration
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Loan Type is required")]
        public int LoanTypeID { get; set; }
        public string? LoanName { get; set; }

        [Required(ErrorMessage = "From Amount is required")]
        public decimal FromLoanAmount { get; set; }

        [Required(ErrorMessage = "To Amount is required")]
        public decimal ToLoanAmount { get; set; }

    }
}
