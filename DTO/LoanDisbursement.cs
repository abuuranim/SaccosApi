namespace SaccosApi.DTO
{
    public class LoanDisbursement
    {
        public Guid LoanApplicationID { get; set; }
        public int BankID { get; set; }
        //public double LoanAmount { get; set; } 
        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }
    }
}
