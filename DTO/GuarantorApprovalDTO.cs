namespace SaccosApi.DTO
{
    public class GuarantorApprovalDTO
    {
        public Guid LoanApplicationID { get; set; }
        public string? approvalStatus { get; set; }

        public string? MembershipNo { get; set; }
    }
}
