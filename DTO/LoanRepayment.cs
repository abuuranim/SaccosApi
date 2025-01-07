namespace SaccosApi.DTO
{
    public class LoanRepayment
    {
        public Guid LoanApplicationID { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public Guid TransactionID { get; set; }
    }
}
