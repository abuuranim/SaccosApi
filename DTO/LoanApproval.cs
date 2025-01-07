namespace SaccosApi.DTO
{
    public class LoanApproval
    {
        public Guid LoanApplicationID { get; set; }
        public string? ApprovalStatus { get; set; }
        public string? ApprovalRemarks { get; set; }
    }
}
