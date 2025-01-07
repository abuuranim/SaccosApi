using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SaccosApi.DTO;
using SaccosApi.Models;
using System.Data;
using System.Net;

namespace SaccosApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class ExpenseController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ExpenseController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        [AllowAnonymous]
        public string Init()
        {
            return "Expense Controller started successfully... ";

        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult addExpense(Expense Expense)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        try
                        {
                            int rowsAffected = dbs.Query<int>("dbo.ExpensesCreate",
                                   new
                                   {
                                       PaymentDate = Expense.PaymentDate,
                                       ItemDescription = Expense.ItemDescription,
                                       Vendor = Expense.Vendor,
                                       Cost = Expense.Cost,
                                       ReferenceNo = Expense.ReferenceNo,
                                       CreatedBy = "SaccosApi"
                                   }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                            return new ObjectResult(new { Data = new { }, StatusCode = HttpStatusCode.OK, Message = "Expense added successfully..." });
                        }
                        catch (Exception ex)
                        {
                            string errorMsg = ex.GetBaseException().Message;
                            throw new Exception(errorMsg);
                        }

                    }
                }
                else { return new ObjectResult(new { StatusCode = HttpStatusCode.BadRequest }); }


            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.BadRequest, Message = errorMsg });
            }

        }

        [HttpPut]
        [AllowAnonymous]
        public IActionResult UpdateExpense(Expense Expense)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        int rowsAffected = dbs.Query<int>("dbo.ExpensesUpdate",
                                new
                                {
                                    ID = Expense.Id,
                                    PaymentDate = Expense.PaymentDate,
                                    ItemDescription = Expense.ItemDescription,
                                    Vendor = Expense.Vendor,
                                    Cost = Expense.Cost,
                                    ReferenceNo = Expense.ReferenceNo,
                                    LastModifiedBy = "SaccosApi"
                                }, commandType: CommandType.StoredProcedure).FirstOrDefault();


                        return new ObjectResult(new { Data = new { }, StatusCode = HttpStatusCode.OK, Message = "Expense updated successfully..." });
                    }
                }
                else { return new ObjectResult(new { StatusCode = HttpStatusCode.BadRequest }); }


            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.BadRequest, Message = errorMsg });
            }

        }

        [HttpDelete]
        [AllowAnonymous]
        public IActionResult RemoveExpense(int Id)
        {
            try
            {
                var query = @" Delete From dbo.Expenses Where Id = @Id";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    dbs.Query(query, new { Id = Id }, commandType: CommandType.Text).FirstOrDefault();

                    return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "Expense removed successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetExpense(int Id)
        {
            try
            {
                var query = @"Select Id, ItemDescription, PaymentDate, Vendor, Cost, ReferenceNo,DateCreated, CreatedBy From Expenses Where Id = @Id";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var expense = dbs.Query<Expense>(query, new { Id = Id }, commandType: CommandType.Text).FirstOrDefault();

                    return new ObjectResult(new { Data = expense, StatusCode = HttpStatusCode.OK, Message = "Expense fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetExpenses(int pageNumber, int pageSize, string? searchTerm, string? sortColumn = "DateCreated", string? sortDirection = "DESC")
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
                        SELECT COUNT(*) FROM dbo.Expenses Where " + whereClause +
                        " Select Id,ItemDescription, PaymentDate, Vendor, Cost, ReferenceNo, DateCreated, CreatedBy From dbo.Expenses " +
                        "Order By DateCreated DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var reader = dbs.QueryMultiple(query, new { Skip = skip, Take = take });
                    int totalRecords = reader.Read<int>().FirstOrDefault();
                    List<Expense> expenses = reader.Read<Expense>().ToList();

                    var result = new PaginationResponse<dynamic>(totalRecords, expenses, pageNumber, pageSize);

                    return Ok(result);
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
