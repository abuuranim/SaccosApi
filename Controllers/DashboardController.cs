using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SaccosApi.Models;
using System.Data;
using System.Net;

namespace SaccosApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class DashboardController: ControllerBase
    {
        private readonly IConfiguration _config;

        public DashboardController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        [AllowAnonymous]
        public string Init()
        {
            return "Member Controller started successfully... ";

        }

        [HttpGet]
        public IActionResult GetAdminDashboardStatistics()
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var data = dbs.Query<dynamic>("dbo.[GetAdminDashboardStatisticsData]", commandType: CommandType.StoredProcedure).FirstOrDefault();
                    return new ObjectResult(new { Data = data, StatusCode = HttpStatusCode.OK, Message = "data fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetMonthlyWiseLoanApplicationTrends()
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var result = dbs.Query<dynamic>("dbo.[GetMonthlyWiseLoanApplicationTrends]", commandType: CommandType.StoredProcedure).ToList();
                    return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "data fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetMemberAppliedLoans()
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
                        var result = dbs.Query<dynamic>("dbo.LoansGetAllMemberAppliedLoans", new { username = username }, commandType: CommandType.StoredProcedure).ToList();

                        return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "Account balances fetched successfully..." });
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
