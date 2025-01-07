using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SaccosApi.DTO;
using SaccosApi.Models;
using System.Data;
using System.Net;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace SaccosApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class SetupController : ControllerBase
    {
        private readonly IConfiguration _config;

    public SetupController(IConfiguration config)
    {
        _config = config;
    }

        [HttpGet]
        public IActionResult BankNames()
        {
            try
            {

                var queryBanks = @"Select [BankID], [BankName] ,[AccountNo] ,[AccountName], [ABBR] FROM [SACCOS].[dbo].[Banks]";
              
                string connectionString = _config["ConnectionStrings:DbContext"];
                var usernameClaim = User.FindFirst("username");
                if (usernameClaim != null)
                {
                    var username = usernameClaim.Value;

                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        var banks = dbs.Query(queryBanks, commandType: CommandType.Text).ToList();

                        return new ObjectResult(new { Data = banks, StatusCode = HttpStatusCode.OK, Message = "Bank names fetched successfully..." });
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
