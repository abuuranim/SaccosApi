using System.ComponentModel.DataAnnotations;

namespace SaccosApi.Models
{
    public class LoanGuarantor
    {
        public Int64 GuarantorID { get; set; }

        [Required(ErrorMessage = "loan applicationID is required")]
        public Guid LoanApplicationID { get; set; }

        [Required(ErrorMessage = "Membership number is required")]
        public string? MembershipNo { get; set; }
        public string? Relationship { get; set; }
        public double GuaranteedAmount { get; set; }
        public string? CreatedBy { get; set; }
        public Nullable<DateTime> DateCreated { get; set; }

    }
}
