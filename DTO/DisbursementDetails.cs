using System.ComponentModel.DataAnnotations;

namespace SaccosApi.DTO
{
    public class DisbursementDetails
    {
        public Guid LoanApplicationID { get; set; }

        public string? MembershipNo { get; set; }

        public int LoanTypeID { get; set; }
        public int LoanTermMonths { get; set; }
        public Nullable<DateTime> ApplicationDate { get; set; }
        public double InterestRate { get; set; }
        public double InsuranceFee { get; set; }
        public double MonthlyPayment { get; set; }
        public double TotalRepaymentAmount { get; set; }
        public Nullable<DateTime> DisbursementDate { get; set; }
        public Nullable<DateTime> DueDate { get; set; }
        public Nullable<DateTime> ApprovalDate { get; set; }
        public decimal LoanAmount { get; set; }
        public string? status { get; set; }
    }
}
