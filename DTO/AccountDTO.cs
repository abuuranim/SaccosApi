using System.ComponentModel.DataAnnotations;

namespace SaccosApi.DTO
{
    public class AccountDTO
    {
        public int AccountTypeID { get; set; }
        public Guid MemberID { get; set; }
        public string? MembershipNo { get; set; }
        public double InitialDepositAmount { get; set; }
        public string? CreatedBy { get; set; }
    }
}
