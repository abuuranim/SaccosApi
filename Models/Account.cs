using System.ComponentModel.DataAnnotations;

namespace SaccosApi.Models
{
    public class Account
    {
        public Int64 AccountID { get; set; }

        [Required(ErrorMessage = "Account Number required")]
        public string? AccountNumber { get; set; }

        [Required(ErrorMessage = "Member ID  required")]
        public Guid MemberID { get; set; }

        public double Balance { get; set; }


        [Required(ErrorMessage = "AccountType ID is required")]
        public int AccountTypeID { get; set; }

        public DateTime DateCreated { get; set; }
    }
}
