using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using Microsoft.VisualBasic;
using SaccosApi.DTO;
using SaccosApi.Models;
using System.Collections.Generic;
using System.Data;
using System.Net;

namespace SaccosApi.Repository
{
    public class LoanRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;
        public LoanRepository(IConfiguration configuration)
        {
            _config = configuration;
            _connectionString = configuration["ConnectionStrings:DbContext"];
        }

        public int ExecuteBatchInsert(List<PaymentScheduleItem> paymentSchedules, LoanDisbursement loanDisbursement)
        {
            using (IDbConnection dbConnection = new SqlConnection(_connectionString))
            {
                dbConnection.Open();
                using (IDbTransaction transaction = dbConnection.BeginTransaction())
                {
                    try
                    {
                        
                        var loanDetails = dbConnection.Query("Select AccountID, t.LoanTypeID, (l.LoanAmount * t.LoanInsuranceFee * 0.01) InsuranceFee, LoanAmount From LoanApplications l Join dbo.LoanTypes t on t.LoanTypeID = l.LoanTypeID Where LoanApplicationID = @LoanApplicationID", new { LoanApplicationID = loanDisbursement.LoanApplicationID}, commandType: CommandType.Text, transaction: transaction).FirstOrDefault();


                        string insertSql = @"
                        INSERT INTO [dbo].[LoanRepaymentSchedules]
                                    ([LoanApplicationID]
                                    ,[PaymentNumber]
                                    ,[PrincipalPayment]
                                    ,[DueDate]
                                    ,[MonthlyPayment]
                                    ,[AmountPaid]
                                    ,[InterestPayment]
                                    ,[RemainingBalance]
                                    ,[RepaymentDate]
                                    ,[PenaltyAppliend]
                                    ,[PenaltyPaid]
                                    ,[status]
                                    ,[DateCreated]
                                    ,[CreatedBy])
                                 VALUES
                                       (@LoanApplicationID
                                       ,@PaymentNumber
                                       ,@PrincipalPayment
                                       ,@DueDate
                                       ,@MonthlyPayment
                                       ,0
                                       ,@InterestPayment
                                       ,@RemainingBalance
                                       ,NULL
                                       ,0
                                       ,0
                                       ,'Pending'
                                       ,GetDate()
                                       ,@CreatedBy)";

                        //1. Record Loan Disbursement Transaction (Execute multiple inserts in a single batch)
                        dbConnection.Execute(insertSql, paymentSchedules, transaction);

                        decimal totalInterestAmount = 0;
                        if (loanDetails?.LoanTypeID == 1)
                        {
                            totalInterestAmount = paymentSchedules.Sum(n => n.InterestPayment);
                        }
                        else
                        {
                            totalInterestAmount = paymentSchedules.First().InterestPayment;
                        }

                        //2. Update Loan Status

                        var affectedRows = dbConnection.Query("Update dbo.LoanApplications set Status = 'Disbursed', InsuranceFee = @InsuranceFee, TotalInterestAmount = @TotalInterestAmount, TotalRepaymentAmount = (@LoanAmount +  @TotalInterestAmount), DisbursementDate = GetDate(), DisbursementRemarks = @DisbursementRemarks   Where LoanApplicationID = @LoanApplicationID", new { LoanApplicationID = loanDisbursement.LoanApplicationID, DisbursementRemarks = loanDisbursement.Remarks, @LoanAmount = loanDetails?.LoanAmount, @InsuranceFee = loanDetails?.InsuranceFee, @TotalInterestAmount = totalInterestAmount }, commandType: CommandType.Text, transaction: transaction).FirstOrDefault();

                        //3. Record Loan Disbursement Transaction
                        var accountID = dbConnection.Query<string>(@"INSERT INTO [dbo].[Transactions] ([TransactionID] ,[AccountID], [BankID], [TransactionDate], [Amount] ,[TransactionType] ,[Description],[CreatedBy])
                                                                    VALUES(@TransactionID, @AccountID, @BankID, @TransactionDate, @Amount, @TransactionType, @Description, @CreatedBy)", new { TransactionID = Guid.NewGuid(), AccountID = loanDetails?.AccountID, BankID = loanDisbursement.BankID, TransactionDate = DateTime.Now, Amount = loanDetails?.LoanAmount, TransactionType = "Credits", Description = "Credits" , CreatedBy = loanDisbursement.CreatedBy }, commandType: CommandType.Text, transaction: transaction).FirstOrDefault();

                        //4. Update Member's Loan Account Balance
                        dbConnection.Query("Update a Set a.Balance = a.Balance + @LoanAmount From LoanApplications l Join Accounts a on l.AccountID = a.AccountID  Where l.LoanApplicationID = @LoanApplicationID", new { LoanApplicationID = loanDisbursement.LoanApplicationID, LoanAmount = loanDetails?.LoanAmount }, commandType: CommandType.Text, transaction: transaction).FirstOrDefault();


                        // Commit the transaction if all commands succeed
                        transaction.Commit();

                        return affectedRows;

                    }
                    catch (Exception ex)
                    {
                        string message = ex.Message;
                        // Rollback the transaction if any command fails
                        transaction.Rollback();
                        throw new Exception(ex.Message);
                    }
                }
            }
        }


        public int LoanRepayment(LoanRepayment loanRepayment)
        {
            using (IDbConnection dbConnection = new SqlConnection(_connectionString))
            {
                dbConnection.Open();
                using (IDbTransaction transaction = dbConnection.BeginTransaction())
                {
                    try
                    {
                        //1

                        string createTransactionSql = @"
                        INSERT INTO [dbo].[Transactions]
                       ([TransactionID]
                       ,[AccountID]
                       ,[TransactionDate]
                       ,[Amount]
                       ,[TransactionType]
                       ,[Description]
                       ,[BankID]
                       ,[CreatedBy]
                       ) VALUES
                        (@TransactionID
                        ,@AccountID
                        ,GetDate()
                        ,@Amount
                        ,'Bank Transfer'
                        ,@Description
                        ,NULL
                        ,@CreatedBy)";

                        //Updating Repayment Record:
                        string insertSql = @"
                        Update [dbo].[LoanRepaymentSchedules]
                                    SET [AmountPaid] =@AmountPaid
                                    ,[PaymentMethod] =@PaymentMethod
                                    ,[PaymentDate] = GetDate()
                                    ,[TransactionID] = @TransactionID
                                    ,[status] = 'Paid'
                                    Where LoanApplicationID = @LoanApplicationID And dbo.MonthSerial(Year(@PaymentDate), Month(@PaymentDate)) =  dbo.MonthSerial(Year(DueDate), Month(DueDate))";

                        //1. Record Loan Disbursement Transaction (Execute multiple inserts in a single batch)
                        dbConnection.Execute(insertSql, "paymentSchedules", transaction);

                        //2. Update Loan Status

                       // var affectedRows = dbConnection.Query("Update dbo.LoanApplications set Status = 'Disbursed', InsuranceFee = @InsuranceFee, TotalRepaymentAmount = (@LoanAmount + @InsuranceFee), DisbursementDate = GetDate(), DisbursementRemarks = @DisbursementRemarks   Where LoanApplicationID = @LoanApplicationID", new { LoanApplicationID = loanDisbursement.LoanApplicationID, DisbursementRemarks = loanDisbursement.Remarks, @LoanAmount = loanDetails?.LoanAmount, @InsuranceFee = loanDetails?.InsuranceFee }, commandType: CommandType.Text, transaction: transaction).FirstOrDefault();

                        //3. Record Loan Disbursement Transaction
                        //var accountID = dbConnection.Query<string>(@"INSERT INTO [dbo].[Transactions] ([TransactionID] ,[AccountID], [BankID], [TransactionDate], [Amount] ,[TransactionType] ,[Description],[CreatedBy])
                                                                    //VALUES(@TransactionID, @AccountID, @BankID, @TransactionDate, @Amount, @TransactionType, @Description, @CreatedBy)", new { TransactionID = Guid.NewGuid(), AccountID = loanDetails?.AccountID, BankID = loanDisbursement.BankID, TransactionDate = DateTime.Now, Amount = loanDetails?.LoanAmount, TransactionType = "Credits", Description = "Credits", CreatedBy = loanDisbursement.CreatedBy }, commandType: CommandType.Text, transaction: transaction).FirstOrDefault();

                        //4. Update Member's Loan Account Balance
                      //  dbConnection.Query("Update a Set a.Balance = a.Balance + @LoanAmount From LoanApplications l Join Accounts a on l.AccountID = a.AccountID  Where l.LoanApplicationID = @LoanApplicationID", new { LoanApplicationID = loanDisbursement.LoanApplicationID, LoanAmount = loanDetails?.LoanAmount }, commandType: CommandType.Text, transaction: transaction).FirstOrDefault();


                        // Commit the transaction if all commands succeed
                        transaction.Commit();

                        return 0;

                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction if any command fails
                        transaction.Rollback();
                        throw new Exception(ex.Message);
                    }
                }
            }
        }

        public async Task<MemberDetailsDTO> GetMemberDetailsAsync(Guid MemberID)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = "Select m.MemberID, u.Username, m.MembershipNo, m.FullName, m.EmailAddress from dbo.Members m Inner Join dbo.Users u on u.Email = m.EmailAddress and u.Id = @MemberID";
                return await db.QueryFirstOrDefaultAsync<MemberDetailsDTO>(sql, new { MemberID = MemberID });
            }
        }

        public async Task<DisbursementDetails> GetDisbursementDetailsAsync(Guid LoanApplicationID)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = @"Select Top 1 l.MembershipNo, s.DueDate, l.LoanApplicationID,l.DisbursementDate, l.LoanAmount, l.DisbursementDate, l.InterestRate, l.TotalRepaymentAmount, l.InsuranceFee, s.MonthlyPayment From 
                              LoanApplications l Join dbo.LoanTypes t on t.LoanTypeID = l.LoanTypeID Join dbo.LoanRepaymentSchedules s on s.LoanApplicationID = l.LoanApplicationID Where l.LoanApplicationID = @LoanApplicationID And l.Status = 'Disbursed' AND s.PaymentNumber = 1";
                return await db.QueryFirstOrDefaultAsync<DisbursementDetails>(sql, new { LoanApplicationID = LoanApplicationID });
            }
        }

        
    }
}
