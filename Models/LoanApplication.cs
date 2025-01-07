using Microsoft.Identity.Client;
using SaccosApi.Models;
using System.ComponentModel.DataAnnotations;

namespace SaccosApi.Models
{
    public class LoanApplication
    {
        public LoanApplication() {
            this.LoanGuarantors = new HashSet<LoanGuarantor>();
        }
        public Guid LoanApplicationID { get; set; }

        [Required(ErrorMessage = "Membership number is required")]
        public string? MembershipNo { get; set; }

        [Required(ErrorMessage = "Loan type is required")]
        public int LoanTypeID { get; set; }

        [Required(ErrorMessage = "Loan tern is required")]
        public int LoanTermMonths { get; set; }
        public DateTime ApplicationDate { get; set; }

        public double InterestRate { get; set; }

        public double InsuranceFee { get; set; }

        public double TotalRepaymentAmount { get; set; }

        public DateTime DisbursementDate { get; set; }
        public string? Purpose { get; set; }

        public Nullable<DateTime> ApprovalDate { get; set; }

        [Required(ErrorMessage = "Loan amount is required")]
        public decimal LoanAmount { get; set; }

        public string? status { get; set; }

        public Attachment Attachment { get; set; }
        public ICollection<LoanGuarantor>? LoanGuarantors { get; set; }
        public string? CreatedBy { get; set; }
        public Nullable<DateTime> DateCreated { get; set; }
        public string? LastModifiedBy { get; set; }
        public Nullable<DateTime> LastModified { get; set; }
    }
}
