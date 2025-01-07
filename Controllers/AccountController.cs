using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SaccosApi.DTO;
using SaccosApi.Models;
using System.Data;
using System.Net;
using System.Net.Mail;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace SaccosApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration _config;


        public AccountController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult GetAccountTypes()
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var accountTypes = dbs.Query(@"Select AccountTypeID, AccountTypeName From  dbo.AccountTypes Where Category Not in ('INCOME')", commandType: CommandType.Text).ToList();


                    return new ObjectResult(new { Data = accountTypes, StatusCode = HttpStatusCode.OK, Message = "Payment types fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetAccountDetails(Guid AccountID)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var result = dbs.Query(@"Select m.FullName, AccountID, AccountNumber, t.AccountTypeName, Balance, DateOpened, a.Status 
                                                   From  dbo.Accounts a Join dbo.Members m on m.MembershipNo = a.MembershipNo Join dbo.AccountTypes t on t.AccountTypeID = a.AccountTypeID 
                                                   Where AccountID = @AccountID", new { AccountID  = AccountID }, commandType: CommandType.Text).FirstOrDefault();


                    return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "Payment types fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetAccountInformation(Guid AccountID)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var result = dbs.Query(@"Select m.FullName, AccountID, AccountNumber, t.AccountTypeName, Balance, DateOpened, a.Status 
                                                   From  dbo.Accounts a Join dbo.Members m on m.MembershipNo = a.MembershipNo Join dbo.AccountTypes t on t.AccountTypeID = a.AccountTypeID 
                                                   Where AccountID = @AccountID", new { AccountID = AccountID }, commandType: CommandType.Text).FirstOrDefault();


                    return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "Payment types fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }


        [HttpGet]
        public IActionResult GetAccountSummary(Guid AccountID)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var result = dbs.Query("dbo.AccountsGetAccountSummary", new { AccountID = AccountID }, commandType: CommandType.StoredProcedure).FirstOrDefault();


                    return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "Payment types fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }




        [HttpGet]
        public IActionResult GetAccountDetailsSummary()
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var result = dbs.Query("AccountsGetMemberAccountsSummary", commandType: CommandType.StoredProcedure).FirstOrDefault();

                    return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "AccountDetails Summary fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult RegisteredAccounts(int pageNumber, int pageSize, string? searchTerm, string? sortColumn = "DateCreated", string? sortDirection = "DESC")
        {
            try
            {
                int maximumPageSize = 100;
                pageSize = pageSize < maximumPageSize ? pageSize : maximumPageSize;
                int skip = (pageNumber - 1) * pageSize;
                int take = pageSize;

                string whereClause = "1 = 1";
                if (searchTerm != null)
                {
                    whereClause = whereClause + " AND ClaimNo LIKE '%" + searchTerm + "%'"
                        + " OR (ClaimMonth  LIKE '%" + searchTerm + "%') AND " + whereClause
                        + " OR (ClaimYear  LIKE '%" + searchTerm + "%') AND " + whereClause;

                }
                var query = @"
                        SELECT COUNT(*) FROM dbo.Accounts Where " + whereClause +
                        "   Select a.AccountID, a.AccountNumber, m.FullName, t.AccountTypeName, t.Category, a.Balance, a.Status, a.DateOpened from Accounts a join AccountTypes t on t.AccountTypeID = a.AccountTypeID join dbo.Members m on m.MembershipNo = a.MembershipNo " +
                        "Order By a.DateOpened DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var reader = dbs.QueryMultiple(query, new { Skip = skip, Take = take });
                    int totalRecords = reader.Read<int>().FirstOrDefault();
                    var accounts = reader.Read().ToList();


                    var result = new PaginationResponse<dynamic>(totalRecords, accounts, pageNumber, pageSize);

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
        public IActionResult MemberAccounts(Guid MemberID)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var accounts = dbs.Query("AccountsGetMemberAccounts",
                                             new { MemberID = MemberID },
                        commandType: CommandType.StoredProcedure).ToList();


                    return new ObjectResult(new { Data = accounts, StatusCode = HttpStatusCode.OK, Message = "Member accounts fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult MemberSavingAccounts(Guid MemberID)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var accounts = dbs.Query("AccountsGetMemberSavingAccounts",
                                             new { MemberID = MemberID },
                        commandType: CommandType.StoredProcedure).ToList();


                    return new ObjectResult(new { Data = accounts, StatusCode = HttpStatusCode.OK, Message = "Member accounts fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetMemberLoanAccounts(Guid MemberID, string AccountStatus)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var accounts = dbs.Query("AccountsGetMemberLoanAccounts",
                                             new { MemberID = MemberID, AccountStatus = AccountStatus },
                        commandType: CommandType.StoredProcedure).ToList();


                    return new ObjectResult(new { Data = accounts, StatusCode = HttpStatusCode.OK, Message = "Member savings account fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

      
        [HttpPost]
        public IActionResult AddNewSavingsAccounts(AccountDTO SavingsAccountDTO)
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
                        var affectedRows = dbs.Query<int>(@"dbo.AccountsCreateNewSavingAccount", 
                            new { MemberID = SavingsAccountDTO.MemberID, 
                                SavingsAccountDTO.AccountTypeID, 
                                InitialDepositAmount = 0,
                                CreatedBy = username }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                        return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "Savings Account created successfully..." });
                    }
                }

                else return new UnauthorizedObjectResult("Unauthorized...");
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpPost]
        public IActionResult CreateAccount(AccountDTO AccountDTO)
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
                        var affectedRows = dbs.Query<int>("dbo.AccountsCreate",
                            new
                            {
                                MembershipNo = AccountDTO.MembershipNo,
                                AccountTypeID = AccountDTO.AccountTypeID,
                                CreatedBy = username
                            }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                        return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "Account created successfully..." });
                    }
                }

                else return new UnauthorizedObjectResult("Unauthorized...");
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }


        [HttpGet]
        //public IActionResult GetCustomerInvoice(string InvoiceNo)
        public IActionResult GetMemberInvoiceDetails()
        {
            try
            {

                var queryBanks = @"Select [BankName] ,[AccountNo] ,[AccountName] FROM [SACCOS].[dbo].[Banks]";
                var queryCompany = @"Select [CompanyName] ,[Email] ,[Phone], [PostalAddress], [PhysicalAddress] FROM [SACCOS].[dbo].[Companies]";
                //var queryInvoiceDetails = @"SELECT i.InvoiceNo, i.InvoiceDate, d.Amount, d.Description FROM Invoices i inner join InvoiceDetails d on d.InvoiceNo = i.InvoiceNo  Where i.InvoiceNo = @InvoiceNo";
                var queryCustomerDetails = @"Select u.FullName, u.Email, m.MembershipNo, m.MobileNo, m.PhysicalAddress from Users u inner join Members m on m.EmailAddress = u.Email Where u.Username = @username";
       
                string connectionString = _config["ConnectionStrings:DbContext"];
                var usernameClaim = User.FindFirst("username");
                if (usernameClaim != null)
                {
                    var username = usernameClaim.Value;

                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        var banks = dbs.Query<Bank>(queryBanks, commandType: CommandType.Text).ToList();
                        var company = dbs.Query<Company>(queryCompany, commandType: CommandType.Text).FirstOrDefault();
                        //var invoiceDetails = dbs.Query<InvoiceDTO>(queryInvoiceDetails, new { InvoiceNo  = InvoiceNo }, commandType: CommandType.Text).ToList();
                        var customerDetails = dbs.Query<CustomerDTO>(queryCustomerDetails, new { username = username }, commandType: CommandType.Text).FirstOrDefault();

                        return new ObjectResult(new { Data = new { Banks = banks, Company = company,  CustomerDetails = customerDetails }, StatusCode = HttpStatusCode.OK, Message = "Expense fetched successfully..." });
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



        [HttpGet]
        public IActionResult GetMemberAkibaAccountBalance() //Akiba ya lazima
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
                        var result = dbs.Query<dynamic>("dbo.AccountGetAkibaAccountBalance", new { username = username }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                        return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "Akiba balance fetched successfully..." });
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


    }
}
