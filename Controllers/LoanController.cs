using ASPNetCoreAuth.Utilities;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using SaccosApi.DTO;
using SaccosApi.Models;
using SaccosApi.Services;
using System;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Net;

namespace SaccosApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class LoanController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly LoanService _loanService;
        private readonly MemberService _memberService;

        public LoanController(IConfiguration config, LoanService loanService , MemberService memberService)
        {
            _config = config;
            _loanService = loanService;
            _memberService = memberService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Init()
        {

            return  new ObjectResult(new { Data = "Loan controller initialized successfully", StatusCode = HttpStatusCode.OK, Message = "Loan types fetched successfully..." });
        }

        [HttpGet]
        public IActionResult GetLoanTypes()
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var loanTypes = dbs.Query(@"Select LoanTypeID, LoanName,LoanInterest,  m.MethodID, m.MethodName, RequireGuarranters From  dbo.LoanTypes l Join InterestCalculationMethods m on m.MethodID = l.InterestCalculationMethodID ", commandType: CommandType.Text).ToList();


                    return new ObjectResult(new { Data = loanTypes, StatusCode = HttpStatusCode.OK, Message = "Loan types fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetLoanTerms(int LoanTypeID)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var loanTerms = dbs.Query(@"Select LoanTypeID, FromLoanAmount, ToLoanAmount, Duration From  dbo.LoanDurations Where LoanTypeID = @LoanTypeID order by FromLoanAmount ASC", new { LoanTypeID = LoanTypeID }, commandType: CommandType.Text).ToList();


                    return new ObjectResult(new { Data = loanTerms, StatusCode = HttpStatusCode.OK, Message = "Loan types fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        

        [HttpGet]
        public async Task<IActionResult> GetMemberLoanApplications(int pageNumber, int pageSize, string? searchTerm, string? sortColumn = "ApplicationDate", string? sortDirection = "DESC")  
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim != null)
                {
                    var userID = userIdClaim.Value;
                    MemberDetailsDTO dto = await _memberService.GetMemberDetailsAsync(Guid.Parse(userID));
     
                    string connectionString = _config["ConnectionStrings:DbContext"];
    
                    int maximumPageSize = 100;
                    pageSize = pageSize < maximumPageSize ? pageSize : maximumPageSize;
                    int skip = (pageNumber - 1) * pageSize;
                    int take = pageSize;

                    string whereClause = "MembershipNo = " + dto.MembershipNo;
                    //if (searchTerm != null)
                    //{
                    //    whereClause = whereClause + " AND ClaimNo LIKE '%" + searchTerm + "%'"
                    //        + " OR (ClaimMonth  LIKE '%" + searchTerm + "%') AND " + whereClause
                    //        + " OR (ClaimYear  LIKE '%" + searchTerm + "%') AND " + whereClause;

                   //}
                var query = @"
                        SELECT COUNT(*) FROM dbo.LoanApplications Where " + whereClause +
                        " Select l.LoanApplicationID, l.MembershipNo, t.LoanName, l.LoanAmount, l.LoanTermMonths as LoanDuration, l.ApplicationDate, l.status from LoanApplications l Join dbo.LoanTypes t on t.LoanTypeID = l.LoanTypeID Where " + whereClause + 
                        "Order By " + sortColumn + " " + sortDirection + " OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {

                        var reader = dbs.QueryMultiple(query, new { Skip = skip, Take = take });
                        int totalRecords = reader.Read<int>().FirstOrDefault();
                        //List<MemberDetails> members = reader.Read<MemberDetails>().ToList();
                        var loans = reader.Read().ToList();

                        var result = new PaginationResponse<dynamic>(totalRecords, loans, pageNumber, pageSize);

                        return Ok(result);
                    }

                }
                else return Unauthorized();

            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }


        [HttpGet]
        [Authorize(Roles = "LOAN_OFFICER")]
        public IActionResult GetPendingLoanApplications(int pageNumber, int pageSize, string? searchTerm, string? sortColumn = "ApplicationDate", string? sortDirection = "DESC")
        {
            try
            {

                    string connectionString = _config["ConnectionStrings:DbContext"];

                    int maximumPageSize = 100;
                    pageSize = pageSize < maximumPageSize ? pageSize : maximumPageSize;
                    int skip = (pageNumber - 1) * pageSize;
                    int take = pageSize;

                    string whereClause = "(t.RequireGuarranters = 0 And l.Status = 'Pending') Or (t.RequireGuarranters = 1 And l.IsApprovedByGuaranters = 1 And l.Status = 'Pending')";
                    //if (searchTerm != null)
                    //{
                    //    whereClause = whereClause + " AND ClaimNo LIKE '%" + searchTerm + "%'"
                    //        + " OR (ClaimMonth  LIKE '%" + searchTerm + "%') AND " + whereClause
                    //        + " OR (ClaimYear  LIKE '%" + searchTerm + "%') AND " + whereClause;

                    //}
                    var query = @"
                        SELECT COUNT(*) FROM dbo.LoanApplications l Join dbo.LoanTypes t on t.LoanTypeID = l.LoanTypeID Where " + whereClause +
                            " Select m.FullName, l.LoanApplicationID, l.MembershipNo, t.LoanName, l.LoanAmount, l.LoanTermMonths as LoanDuration, l.ApplicationDate, l.status from LoanApplications l Join dbo.Members m on m.MembershipNo = l.MembershipNo Join dbo.LoanTypes t on t.LoanTypeID = l.LoanTypeID Where " + whereClause +
                            "Order By " + sortColumn + " " + sortDirection + " OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {

                        var reader = dbs.QueryMultiple(query, new { Skip = skip, Take = take });
                        int totalRecords = reader.Read<int>().FirstOrDefault();
                        var loans = reader.Read().ToList();

                        var result = new PaginationResponse<dynamic>(totalRecords, loans, pageNumber, pageSize);

                        return Ok(result);
                    }



            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetLoanDisbursementDetails(Guid loanApplicationID)
        {
            try
            {
                var application = await _loanService.GetDisbursementDetailsAsync(loanApplicationID);

                return new ObjectResult(new { Data = application, StatusCode = HttpStatusCode.OK, Message = "loan disbursements fetched successfully..." });
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public async Task<IActionResult>  GetLoanApplicationSummary(Guid LoanApplicationID)
        {
            try
            {
               var application = await _loanService.GetLoanApplicationSummary(LoanApplicationID);

                return new ObjectResult(new { Data = application, StatusCode = HttpStatusCode.OK, Message = "loan application submitted successfully..." });
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet("{loanApplicationID}")]
        [AllowAnonymous]
        public async Task<IActionResult> PreviewAttachment(Guid loanApplicationID)
        {


            string connectionString = _config["ConnectionStrings:DbContext"];
            using (SqlConnection dbs = new SqlConnection(connectionString))
            {
                try
                {
                    var path = dbs.Query("Select FilePath From dbo.Attachments Where LoanApplicationID = @LoanApplicationID", new { LoanApplicationID = loanApplicationID }, commandType: CommandType.Text).FirstOrDefault();
                    if (!System.IO.File.Exists(path?.FilePath))
                    {
                        return NotFound();
                    }

                    var memory = new MemoryStream();
                    using (var stream = new FileStream(path?.FilePath, FileMode.Open))
                    {
                        await stream.CopyToAsync(memory);
                    }

                    memory.Position = 0;
                    return File(memory, "application/pdf", "fileName.pdf");
                    // return File(memory, GetContentType(path?.FilePath), Path.GetFileName(path?.FilePath));
                    //
                }
                catch (Exception exception) {
                    string errorMsg = exception.GetBaseException().Message;
                    return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
                }


            }

        }

        [HttpGet]
        public async Task<IActionResult> GetLoanDetails(Guid LoanApplicationID)
        {
           var query = @"Select m.FullName, t.LoanName, a.LoanAmount, a.LoanTermMonths as LoanTerms, a.Purpose, a.ApplicationDate, a.status from LoanApplications a inner join LoanTypes t on t.LoanTypeID = a.LoanTypeID inner Join dbo.Members m on m.MembershipNo = a.MembershipNo
                             Where LoanApplicationID = @LoanApplicationID";
           string connectionString = _config["ConnectionStrings:DbContext"];
           var userIdClaim = User.FindFirst("userId");
            if (userIdClaim != null)
            {
                MemberDetailsDTO dto = await _memberService.GetMemberDetailsAsync(Guid.Parse(userIdClaim?.Value));

                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    dbs.Open();
                    SqlTransaction trans = dbs.BeginTransaction();
                    try
                    {
                        var affectedRows = dbs.Query("Update dbo.Notifications set IsRead = 1 Where LoanApplicationID = @LoanApplicationID And MembershipNo = @MembershipNo", new { LoanApplicationID = LoanApplicationID, MembershipNo = dto?.MembershipNo }, commandType: CommandType.Text, transaction: trans).FirstOrDefault();

                        var loanDetails = dbs.Query<dynamic>(query, new { LoanApplicationID = LoanApplicationID }, commandType: CommandType.Text, transaction: trans).FirstOrDefault();

                        trans.Commit();

                        return new ObjectResult(new { Data = loanDetails, StatusCode = HttpStatusCode.OK, Message = "Member details fetched successfully..." });
                    }
                    catch (Exception exception)
                    {
                        trans.Rollback();
                        string errorMsg = exception.GetBaseException().Message;
                        return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
                    }
                }
            }
            else return Unauthorized();


        }

        [HttpPost]
        public IActionResult GuarantorApproval(GuarantorApprovalDTO GuarantorApproval)
        {
            try
            {

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var details = dbs.Query("dbo.LoanGuarantorsApproval", new { MembershipNo = GuarantorApproval.MembershipNo, ApprovalStatus = GuarantorApproval.approvalStatus, LoanApplicationID = GuarantorApproval.LoanApplicationID }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                    return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "guarantor approval executed successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpPost]
        public IActionResult LoanApproval(LoanApproval LoanApproval)
        {
            try
            {
                var usernameClaim = User.FindFirst("username");
                var fullNameClaim = User.FindFirst("fullName");
                if (usernameClaim != null && fullNameClaim != null)
                {
                    var username = usernameClaim.Value;
                    var fullName = fullNameClaim.Value;
       
                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                var loanDetails = connection.Query("Select a.MembershipNo, a.LoanTypeID, t.AccountTypeID From LoanApplications a Join dbo.LoanTypes t on t.LoanTypeID = a.LoanTypeID  Where LoanApplicationID = @LoanApplicationID", new { LoanApplicationID = LoanApproval.LoanApplicationID}, commandType: CommandType.Text, transaction: transaction).FirstOrDefault();
                                // 1. Update loan status to approved
                                connection.Query("UPDATE LoanApplications  SET status = @ApprovalStatus, ApprovalDate = GETDATE(), ApprovedBy = @ApprovedBy, ApprovalRemarks = @ApprovalRemarks WHERE LoanApplicationID = @LoanApplicationID", new { ApprovalStatus = LoanApproval.ApprovalStatus, LoanApplicationID = LoanApproval.LoanApplicationID, ApprovalRemarks = LoanApproval.ApprovalRemarks, ApprovedBy = fullName }, commandType: CommandType.Text, transaction: transaction).FirstOrDefault();
                                
                                //2. Create Account
                                if(LoanApproval?.ApprovalStatus?.IndexOf("Approved") >= 0)
                                {
                                    var result = connection.Query<dynamic>("dbo.AccountsCreate", new { MembershipNo = loanDetails?.MembershipNo, AccountTypeID = loanDetails?.AccountTypeID, CreatedBy = username }, commandType: CommandType.StoredProcedure, transaction: transaction).FirstOrDefault();
                                    //connection.Execute("dbo.AccountsCreate", new { MembershipNo = loanDetails?.MembershipNo, AccountTypeID = loanDetails?.AccountTypeID, CreatedBy = username }, commandType: CommandType.StoredProcedure, transaction: transaction);
                                   //Set LoanAccount
                                    connection.Execute("UPDATE LoanApplications  SET AccountID = @AccountID WHERE LoanApplicationID = @LoanApplicationID", new { @AccountID = result?.AccountID, LoanApplicationID = LoanApproval.LoanApplicationID }, commandType: CommandType.Text, transaction: transaction);
                                }


                                // Commit the transaction if everything is successful
                                transaction.Commit();
                                return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "loan application approved successfully..." });
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw new Exception(ex.GetBaseException().Message);
                            }
                        }


                    }
                }
                else return Unauthorized();

               
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpPost]
        [Authorize(Roles = "MANAGER, LOAN_OFFICER")]
        public IActionResult LoanDisbursement(LoanDisbursement loanDisbursement)
        {
            try
            {
                var usernameClaim = User.FindFirst("username");
                if (usernameClaim != null)
                {
                    var username = usernameClaim.Value;
                    loanDisbursement.CreatedBy = username;
                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
 
                        try
                        {
                 
                            var queryLoanDetails = @"Select t.InterestCalculationMethodID, t.loanInterest as AnnualInterestRate, a.LoanAmount as Principal, a.LoanTermMonths as TotalPayments From LoanApplications a inner join LoanTypes t on t.LoanTypeID = a.LoanTypeID
                                Where LoanApplicationID = @LoanApplicationID AND a.Status = 'Approved'";
                      
                            var loanDetails = dbs.Query<dynamic>(queryLoanDetails, new { LoanApplicationID = loanDisbursement.LoanApplicationID }, commandType: CommandType.Text).FirstOrDefault();

                            if (loanDetails?.InterestCalculationMethodID == 1)
                            {
                                List<PaymentScheduleItem> schedule = GeneratePaymentScheduleForStraightLine(loanDetails?.Principal, loanDetails?.AnnualInterestRate, loanDetails?.TotalPayments, loanDisbursement.LoanApplicationID, username);
                                _loanService?.DisburseLoan(schedule, loanDisbursement);
                            }
                            else if (loanDetails?.InterestCalculationMethodID == 2) //Reducing Balance Method of Amortization
                            {
                                List<PaymentScheduleItem> schedule = GeneratePaymentScheduleForReducedBalance(loanDetails?.Principal, loanDetails?.AnnualInterestRate, loanDetails?.TotalPayments, loanDisbursement.LoanApplicationID, username);
                                _loanService?.DisburseLoan(schedule, loanDisbursement);
                            }

                           
                            return new ObjectResult(new { Data = loanDetails, StatusCode = HttpStatusCode.OK, Message = "Loan disbursed successfully..." });
                        }
                        catch (Exception exception)
                        {
                            string errorMsg = exception.GetBaseException().Message;
                            return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
                        }

                    }
                }

                else return Unauthorized();
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetLoanApplicationDetails(Guid LoanApplicationID)
        {
            try
            {
                var query = @"Select a.LoanApplicationID, a.AccountID, t.LoanName, a.LoanAmount, a.LoanTermMonths as LoanTerms, a.Purpose, a.ApplicationDate, c.AccountNumber, a.Status from LoanApplications a inner join LoanTypes t on t.LoanTypeID = a.LoanTypeID
                              Left Join dbo.Accounts c on c.AccountID = a.AccountID Where LoanApplicationID = @LoanApplicationID";

                var guarantersQuery = @"Select m.FullName, m.MobileNo, m.EmailAddress, g.ApprovalStatus, g.ApprovalDate From LoanGuarantors g inner join Members m on m.MembershipNo = g.MembershipNo Where LoanApplicationID =  @LoanApplicationID";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var loanDetails = dbs.Query<dynamic>(query, new { LoanApplicationID = LoanApplicationID }, commandType: CommandType.Text).FirstOrDefault();
                    var guarantersDetails = dbs.Query<dynamic>(guarantersQuery, new { LoanApplicationID = LoanApplicationID }, commandType: CommandType.Text).ToList();

                    return new ObjectResult(new { Data = new { LoanDetails = loanDetails, Guaranters = guarantersDetails }, StatusCode = HttpStatusCode.OK, Message = "Member details fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetLoanRepaymentSchedules(Guid LoanApplicationID)
        {
            try
            {
                var query = @"Select PaymentNumber, MonthlyPayment, AmountPaid, RemainingBalance, DueDate, Status FROM LoanRepaymentSchedules Where LoanApplicationID = @LoanApplicationID";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var result = dbs.Query<dynamic>(query, new { LoanApplicationID = LoanApplicationID }, commandType: CommandType.Text).ToList();

                    return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "loan repayment schedules fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }


        [HttpPost]
        [AllowAnonymous]
        public IActionResult LoanCalculationByStraightLinerMethod(LoanCalculator LoanCalculator)
        {
            try
            {
                var usernameClaim = "";//User.FindFirst("username");
                if (usernameClaim != null)
                {
                    var N = 12; //Number of Months in year
                    LoanCalculator.MonthlyInterestRate = LoanCalculator.AnnualInterestRate / N;
                    LoanCalculator.TotalInterest = (LoanCalculator.LoanAmount * LoanCalculator.AnnualInterestRate * LoanCalculator.LoanDuration) / (N * 100);
                    LoanCalculator.TotalPayment = LoanCalculator.TotalInterest + LoanCalculator.LoanAmount;
                    //LoanCalculator.MonthlyPayment = Math.Round( (LoanCalculator.TotalPayment / LoanCalculator.LoanDuration), 2);
                    LoanCalculator.MonthlyPayment = LoanCalculator.TotalPayment / LoanCalculator.LoanDuration;
                    return new ObjectResult(new { Data = LoanCalculator, StatusCode = HttpStatusCode.OK, Message = "loan calculation done successfully..." });

                }

                else return new UnauthorizedResult();
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult LoanCalculationByReducedBalanceMethod(LoanCalculator LoanCalculator)
        {
            try
            {
                var usernameClaim = "";//User.FindFirst("username");
                if (usernameClaim != null)
                {
    
                    double Principal = LoanCalculator.LoanAmount;
                    int NumPayments = LoanCalculator.LoanDuration;
                    LoanCalculator.MonthlyInterestRate = LoanCalculator.AnnualInterestRate / 12 / 100;
                    LoanCalculator.MonthlyInterestAmount = (LoanCalculator.LoanAmount * LoanCalculator.AnnualInterestRate)/ 12 / 100;

                    LoanCalculator.MonthlyPayment = Principal * (LoanCalculator.MonthlyInterestRate * (double)Math.Pow((double)(1 + LoanCalculator.MonthlyInterestRate), NumPayments)) /
                           ((double)Math.Pow((double)(1 + LoanCalculator.MonthlyInterestRate), NumPayments) - 1);

                    return new ObjectResult(new { Data = LoanCalculator, StatusCode = HttpStatusCode.OK, Message = "loan calculation done successfully..." });

                }

                else return new UnauthorizedResult();
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }


        [HttpPost]
        public async Task<IActionResult> SubmitLoanApplicationAsync(LoanApplication LoanApplication)
        {
            try
            {
                var usernameClaim = User.FindFirst("username");
                if (usernameClaim != null)
                {
                    var username = usernameClaim.Value;

                    var result = await _loanService.SubmitLoanApplicationAsync(LoanApplication, username);
                    
                    return new ObjectResult(new {Data = result, StatusCode = HttpStatusCode.OK, Message = "loan application submitted successfully..." });

                }

                else return new UnauthorizedResult();
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        //Payment Schedule
        [HttpGet]
        [Authorize(Roles = "MANAGER, LOAN_OFFICER")]
        public IActionResult GetMemberLoanBalances(Guid MemberID)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var loanBalances = dbs.Query("dbo.LoanApplicationsGetPendingLoans",
                                             new { MemberID = MemberID },
                        commandType: CommandType.StoredProcedure).ToList();


                    return new ObjectResult(new { Data = loanBalances, StatusCode = HttpStatusCode.OK, Message = "Member accounts fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        private List<PaymentScheduleItem> GeneratePaymentScheduleForStraightLine(decimal principal, decimal annualInterestRate, int loanTerm, Guid loanApplicationID, string createdBy)
        {
            DateTime dueDate = DateTime.Now;
            DateTime todayDate = DateTime.Now;
            if(todayDate.Day >= 15)
            {
                dueDate = todayDate.AddMonths(1);
            }
  

            List<PaymentScheduleItem> schedule = new List<PaymentScheduleItem>();

            decimal interestAmount = (principal * annualInterestRate * loanTerm) / 100;
            decimal totalPayment = interestAmount + principal;
            decimal monthlyPayment = totalPayment / loanTerm;

            decimal remainingBalance = totalPayment; 

            for (int i = 1; i <= loanTerm; i++)
            {
                remainingBalance -= monthlyPayment;

                // Create PaymentScheduleItem
                PaymentScheduleItem item = new PaymentScheduleItem
                {
                    LoanApplicationID = loanApplicationID,
                    PaymentNumber = i,
                    DueDate = Helper.GetLastDayOfMonth(dueDate.AddMonths(i - 1)),
                    PrincipalPayment = principal,
                    InterestPayment = interestAmount,
                    MonthlyPayment = monthlyPayment,
                    RemainingBalance = remainingBalance,
                    CreatedBy = createdBy
                };
               
                schedule.Add(item);
            }

            return schedule;
        }

        private List<PaymentScheduleItem> GeneratePaymentScheduleForReducedBalance(decimal principal, decimal annualInterestRate, int totalPayments, Guid loanApplicationID, string createdBy)
        {
            DateTime dueDate = DateTime.Now;
            DateTime todayDate = DateTime.Now;
            if (todayDate.Day >= 15)
            {
                dueDate = todayDate.AddMonths(1);
            }


            List<PaymentScheduleItem> schedule = new List<PaymentScheduleItem>();

            // Calculate monthly interest rate
            decimal monthlyInterestRate = annualInterestRate / 100 / 12;

            // Calculate monthly payment using the formula
            decimal powerFactor = (decimal)Math.Pow(1 + (double)monthlyInterestRate, totalPayments);
            decimal monthlyPayment = principal * (monthlyInterestRate * powerFactor) / (powerFactor - 1);

            decimal currentBalance = principal;

            for (int i = 1; i <= totalPayments; i++)
            {
                // Calculate interest payment
                decimal interestPayment = currentBalance * monthlyInterestRate;

                // Calculate principal payment
                decimal principalPayment = monthlyPayment - interestPayment;

                // Update remaining balance
                currentBalance -= principalPayment;


                // Create PaymentScheduleItem
                PaymentScheduleItem item = new PaymentScheduleItem
                {
                    LoanApplicationID = loanApplicationID,
                    PaymentNumber = i,
                    DueDate = Helper.GetLastDayOfMonth(dueDate.AddMonths(i - 1)),
                    PrincipalPayment = principalPayment,
                    InterestPayment = interestPayment,
                    MonthlyPayment = monthlyPayment,
                    RemainingBalance = currentBalance,
                    CreatedBy = createdBy
                };


                // Add to schedule
                schedule.Add(item);
            }

            return schedule;
        }


        [HttpGet]
        public IActionResult GetLoanPaymentSchedules(Guid loanApplicationID)
        {
            try
            {
                var query = @"Select PaymentNumber, PrincipalPayment, MonthlyPayment,AmountPaid, RemainingBalance, DueDate, RepaymentDate as PaymentDate, Status From LoanRepaymentSchedules Where LoanApplicationID = @LoanApplicationID";
   
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var schedules = dbs.Query<dynamic>(query, new { LoanApplicationID = loanApplicationID }, commandType: CommandType.Text).ToList();
                    return new ObjectResult(new { Data = schedules, StatusCode = HttpStatusCode.OK, Message = "repayment schedules fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

       
        private List<PaymentScheduleItem> GeneratePaymentSchedule(decimal principal, decimal annualInterestRate, int totalPayments, DateTime startDate, string createdBy)
        {
            List<PaymentScheduleItem> schedule = new List<PaymentScheduleItem>();

            // Calculate monthly interest rate
            decimal monthlyInterestRate = annualInterestRate / 100 / 12;

            // Calculate monthly payment using declining balance method
            decimal monthlyPayment = (principal * monthlyInterestRate) / (1 - (decimal)Math.Pow((double)(1 + monthlyInterestRate), -totalPayments));

            decimal currentBalance = principal;

            for (int i = 1; i <= totalPayments; i++)
            {
                // Calculate interest payment
                decimal interestPayment = currentBalance * monthlyInterestRate;

                // Calculate principal payment
                decimal principalPayment = monthlyPayment - interestPayment;

                // Update remaining balance
                currentBalance -= principalPayment;

                // Create PaymentScheduleItem
                PaymentScheduleItem item = new PaymentScheduleItem
                {
                    PaymentNumber = i,
                    PaymentDate = startDate.AddMonths(i - 1),
                    PrincipalPayment = principalPayment,
                    InterestPayment = interestPayment,
                    MonthlyPayment = monthlyPayment,
                    RemainingBalance = currentBalance,
                    CreatedBy  = createdBy
                };

                // Add to schedule
                schedule.Add(item);
            }

            return schedule;
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
        {
            //{ ".txt", "text/plain" },
            { ".pdf", "application/pdf" },
            //{ ".doc", "application/vnd.ms-word" },
            //{ ".docx", "application/vnd.ms-word" },
            //{ ".xls", "application/vnd.ms-excel" },
            //{ ".xlsx", "application/vnd.openxmlformats officedocument.spreadsheetml.sheet" },
            //{ ".png", "image/png" },
            //{ ".jpg", "image/jpeg" },
            //{ ".jpeg", "image/jpeg" },
            //{ ".gif", "image/gif" },
            //{ ".csv", "text/csv" }
        };
        }
    }
}
