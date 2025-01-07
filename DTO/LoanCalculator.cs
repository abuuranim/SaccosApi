namespace SaccosApi.DTO
{
    public class LoanCalculator
    {
        public double AnnualInterestRate { get; set; }
        public double MonthlyInterestRate { get; set; }
        public double LoanAmount { get; set; }
        public string? LoanProductCode { get; set; }
        public int LoanDuration { get; set; }
        public int RepaymentFrequency { get; set; }

        /// <summary>
        /// Result
        /// </summary>
        public double MonthlyPayment { get; set; }
        public double MonthlyInterestAmount { get; set; }
        public double TotalInterest { get; set; }
        public double TotalPayment { get; set; }
    }
}

