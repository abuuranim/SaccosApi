using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic.FileIO;
using SaccosApi.DTO;
using SaccosApi.Models;
using SaccosApi.Repository;
using System.Data;
using System.Runtime.CompilerServices;

namespace SaccosApi.Services
{
    public class LoanService
    {
        private readonly LoanRepository _loanRepository;
        private readonly IConfiguration _configuration;

        public LoanService(IConfiguration configuration,LoanRepository loanRepository)
        {
            _loanRepository = loanRepository;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<object> GetLoanApplicationSummary(Guid LoanApplicationID)
        {
            try
            {
                string connectionString = _configuration["ConnectionStrings:DbContext"];
                using (var connection = new SqlConnection(connectionString))
                {
                    var application = connection.Query(@"
                        select t.LoanName, l.LoanAmount, l.Purpose, LoanTermMonths, l.ApplicationDate, l.Status from LoanApplications l Join LoanTypes t on t.LoanTypeID = l.LoanTypeID Where LoanApplicationID = @LoanApplicationID", new { LoanApplicationID = LoanApplicationID }, commandType: CommandType.Text).FirstOrDefault();


                    return application;
                }
            }
            catch (Exception exception)
            {
                throw;
            }

        }


        [HttpGet]
        public async Task<object> GetDisbursementDetailsAsync(Guid LoanApplicationID)
        {
            try
            {
              return await _loanRepository.GetDisbursementDetailsAsync(LoanApplicationID);
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<object> SubmitLoanApplicationAsync(LoanApplication loanApplication, string username)
        {
            string connectionString = _configuration["ConnectionStrings:DbContext"];

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {

                        var attachmentID = loanApplication.MembershipNo;
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Attachments\\", attachmentID + "." + loanApplication?.Attachment?.FileType);

                        // Insert loan application

                        var result = connection.Query("LoanApplicationsSubmit",
                        new
                        {
                            MembershipNo = loanApplication.MembershipNo,
                            LoanTypeID = loanApplication.LoanTypeID,
                            LoanAmount = loanApplication.LoanAmount,
                            LoanTermMonths = loanApplication.LoanTermMonths,
                            InterestRate = loanApplication.InterestRate,
                            Purpose = loanApplication.Purpose,
                            CreatedBy = username
                        },
                      
                        commandType: CommandType.StoredProcedure,transaction: transaction).FirstOrDefault();

                        // Insert guarantors
                        //string insertGuarantorsSql = @"
                        //INSERT INTO LoanGuarantors (LoanApplicationID, MembershipNo, GuaranteedAmount,ApprovalStatus, CreatedBy)
                        //VALUES (@LoanApplicationId, @MembershipNo, @GuaranteedAmount, @ApprovalStatus, @CreatedBy);";

                        if(loanApplication?.LoanGuarantors?.Count > 0)
                        foreach (var guarantor in loanApplication.LoanGuarantors)
                        {
                            await connection.ExecuteAsync("dbo.LoanGuarantersCreate", 
                            new {
                                   LoanApplicationID = result.LoanApplicationID,
                                   MembershipNo = guarantor.MembershipNo,
                                   GuaranteedAmount = loanApplication.LoanAmount,
                                   CreatedBy = username
                            }, commandType: CommandType.StoredProcedure, transaction: transaction);

                        }



                        string insertSql = @"
                        INSERT INTO Attachments (AttachmentID, LoanApplicationID, AttachmentType, FileName, FileType, FilePath, UploadedBy)
                        VALUES (@AttachmentID, @LoanApplicationID, @AttachmentType, @FileName, @FileType, @FilePath, @UploadedBy);";

                        
                        await connection.ExecuteAsync(insertSql, new { AttachmentID = Guid.NewGuid(), LoanApplicationID = result?.LoanApplicationID, AttachmentType="SalarySlip", FileName = loanApplication?.Attachment.FileName, FileType = loanApplication?.Attachment.FileType, FilePath = filePath, UploadedBy  = username}, transaction);

                        byte[] imageBytes = Convert.FromBase64String(loanApplication.Attachment.FileContent);
                        System.IO.FileInfo file = new System.IO.FileInfo(filePath);
                        file.Directory.Create();
                        System.IO.File.WriteAllBytes(file.FullName, imageBytes);

                        // Commit transaction
                        transaction.Commit();

                        return result;
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction in case of error
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public  async Task<int> DisburseLoan(List<PaymentScheduleItem> paymentSchedules, LoanDisbursement loanDisbursement)
        {
            try
            {
                return _loanRepository.ExecuteBatchInsert(paymentSchedules, loanDisbursement);
               
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }

        }
    }
}
