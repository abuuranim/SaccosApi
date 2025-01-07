using System.Runtime.ConstrainedExecution;

namespace SaccosApi.Models
{
    public class PaymentScheduleItem
    {
        public Guid LoanApplicationID { get; set; }
        public int PaymentNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal PrincipalPayment { get; set; }
        public decimal InterestPayment { get; set; }
        public decimal MonthlyPayment { get; set; }
        public decimal RemainingBalance { get; set; }
        public DateTime DueDate { get; set; }
        public decimal AmountPaid { get; set;}
        public string? Status { get; set; }

        public string? CreatedBy { get; set; }


    }
}
