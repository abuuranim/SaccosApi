using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaccosApi.Models;
using System.Data;
using System.Net;
using Dapper;
using SaccosApi.DTO;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;

namespace SaccosApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly IConfiguration _config;

        public TransactionController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult GetApplicationFees()
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var shareCapital = dbs.Query(@"Select AccountTypeID, AccountTypeName, Alias, MinimumAmount, MaximumAmount, UnitPrice, Duration From  
                                                     dbo.AccountTypes Where Alias ='SHARE_CAPITAL'", commandType: CommandType.Text).FirstOrDefault();

                    var applicationFee = dbs.Query(@"Select Top 1 ApplicationFee from dbo.ApplicationFees order by StartDate desc", commandType: CommandType.Text).FirstOrDefault();

                    return new ObjectResult(new { Data = new { ShareCapital = shareCapital, ApplicationFee = applicationFee }, StatusCode = HttpStatusCode.OK, Message = "Application fees fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetPaymentTypes()
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var paymentTypes = dbs.Query(@"Select PaymentTypeID, PaymentType From  dbo.PaymentTypes", commandType: CommandType.Text).ToList();

       
                    return new ObjectResult(new { Data = paymentTypes, StatusCode = HttpStatusCode.OK, Message = "Payment types fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        

        [HttpPost]
        public IActionResult CreateInvoice(Invoice Invoice)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                var userIdClaim = User.FindFirst("userId");
                var usernameClaim = User.FindFirst("username");

                if (usernameClaim != null && userIdClaim != null)
                {
                    var username = usernameClaim.Value;
                    var userID = userIdClaim.Value;

                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        dbs.Open();
                        SqlTransaction trans = dbs.BeginTransaction();
                        try
                        {
                            var invoice = dbs.Query("InvoicesCreate",
                            new
                            {
                                UserId = userID,
                                InvoiceDate = DateTime.Now,
                                InvoiceDescription = Invoice?.InvoiceDescription,
                                InvoiceAmount = Invoice?.InvoiceAmount,
                                CreatedBy = username
                            }, commandType: CommandType.StoredProcedure, transaction: trans).FirstOrDefault();

                            if (invoice != null)
                            {
                                foreach (InvoiceDetails details in Invoice.InvoiceDetails)
                                {
                                    dbs.Query("InvoiceDetailsCreate",
                                     new
                                     {
                                         InvoiceNo = invoice.InvoiceNo,
                                         Description = details?.Description,
                                         Amount = details?.Amount,
                                         CreatedBy = username
                                     }, commandType: CommandType.StoredProcedure, transaction: trans).FirstOrDefault();
                                }

                                dbs.Query("UsersUpdateStage",
                                     new
                                     {
                                         Username = username,
                                         StageAlias = "PURCHASING_SHARES"
                                     }, commandType: CommandType.StoredProcedure, transaction: trans).FirstOrDefault();


                                trans.Commit();
                            }
                            return new ObjectResult(new { InvoiceNo = invoice?.InvoiceNo, StatusCode = HttpStatusCode.OK, Message = "Invoice posted successfully..." });

                        }
                        catch(Exception e) { 
                            trans.Rollback();
                            throw e;
                        }
                        
                    }

                   
                }

                else
                {
                    return Unauthorized();
                }


               
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpPost]
        public IActionResult DepositFunds(Transaction Transaction)
        {
            try
            {

                    string connectionString = _config["ConnectionStrings:DbContext"];
                    var usernameClaim = User.FindFirst("username");
                    if (usernameClaim != null)
                    {
                        var username = usernameClaim.Value;
                        using (SqlConnection dbs = new SqlConnection(connectionString))
                        {
     
                            var result = dbs.Query("dbo.DepositFunds",
                                new
                                {
 
                                    AccountID = Transaction?.AccountID,
                                    DepositAmount = Transaction?.TransactionAmount,
                                    PaymentMethod = Transaction?.PaymentMethod,
                                    Description = Transaction?.Description,
                                    CreatedBy = username
                                }, commandType: CommandType.StoredProcedure).FirstOrDefault();
                                  

                            return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "Transaction posted successfully..." });
            
                        }
                    }

                    else { return new BadRequestObjectResult(new { StatusCode = HttpStatusCode.Unauthorized }); } 
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.BadRequest, Message = errorMsg });
            }

        }

        [HttpPost]
        public IActionResult WithDrawFunds(Transaction Transaction)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                var usernameClaim = User.FindFirst("username");
                if (usernameClaim != null)
                {
                    var username = usernameClaim.Value;
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {

                        var result = dbs.Query("dbo.WithdrawFunds",
                            new
                            {

                                AccountID = Transaction?.AccountID,
                                WithdrawAmount = Transaction?.TransactionAmount,
                                withdrawalDate = Transaction?.TransactionDate,
                                Description = Transaction?.Description,
                                CreatedBy = username
                            }, commandType: CommandType.StoredProcedure).FirstOrDefault();


                        return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "Transaction posted successfully..." });

                    }
                }

                else { return new BadRequestObjectResult(new { StatusCode = HttpStatusCode.Unauthorized }); }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.BadRequest, Message = errorMsg });
            }

        }

        [HttpPost]
        public IActionResult LoanRepayment(Transaction Transaction)
        {
            try
            {

                string connectionString = _config["ConnectionStrings:DbContext"];
                var usernameClaim = User.FindFirst("username");
                if (usernameClaim != null)
                {
                    var username = usernameClaim.Value;
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {

                        var result = dbs.Query("dbo.LoanRepayment",
                            new
                            {

                                AccountID = Transaction?.AccountID,
                                AmountPaid = Transaction?.TransactionAmount,
                                PaymentMethod = Transaction?.PaymentMethod,
                                RepaymentDate = Transaction?.TransactionDate,
                                Description = Transaction?.Description,
                                CreatedBy = username
                            }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                        return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "Transaction posted successfully..." });

                    }
                }

                else { return new BadRequestObjectResult(new { StatusCode = HttpStatusCode.Unauthorized }); }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.BadRequest, Message = errorMsg });
            }

        }
        [HttpGet]
        public IActionResult GetMostRecentlyTransactions(Guid AccountID)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var result = dbs.Query("dbo.TransactionsGetMostRecentlyTransactions", new { AccountID = AccountID }, commandType: CommandType.StoredProcedure).ToList();


                    return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "transactions fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }


        [HttpGet]
        public IActionResult GetMemberTransactionHistory(Guid MemberID)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var accounts = dbs.Query("TransactionsGetMemberTransactionHistory",
                                             new { MemberID = MemberID },
                        commandType: CommandType.StoredProcedure).ToList();


                    return new ObjectResult(new { Data = accounts, StatusCode = HttpStatusCode.OK, Message = "Member hisa account fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

    }
}
