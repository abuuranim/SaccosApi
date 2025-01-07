using ASPNetCoreAuth.Models;
using ASPNetCoreAuth.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Net;
using SaccosApi.Models;
using Dapper;
using SaccosApi.DTO;
using Microsoft.Data.SqlClient;

namespace SaccosApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class LoanTypeController : ControllerBase
    {
        private readonly IConfiguration _config;

        public LoanTypeController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        [AllowAnonymous]
        public string Init()
        {
            return "Loan Type Controller started successfully... ";

        }

        

        [HttpGet]
        [ActionName("SelectLoanTypes")]
        public IActionResult GetLoanTypes()
        {
            try
            {
                var query = @"Select LoanTypeID, LoanName From dbo.LoanTypes";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var loanTypes = dbs.Query(query, commandType: CommandType.Text).ToList();

                    return new ObjectResult(new { Data = loanTypes, StatusCode = HttpStatusCode.OK, Message = "loan types fetched successfully..." });
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
        public IActionResult GetInterestCalculationMethods()
        {
            try
            {
                var query = @" Select MethodID, MethodName From dbo.InterestCalculationMethods";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var methods = dbs.Query(query, commandType: CommandType.Text).ToList();

                    return new ObjectResult(new { Data = methods, StatusCode = HttpStatusCode.OK, Message = "Interest calculation methods fetched successfully..." });
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
        public IActionResult GetLoanType(int LoanTypeID)
        {
            try
            {
                var query = @" Select LoanTypeID, m.MethodName, interestCalculationMethodID, LoanName, LoanInterest, ProcessingFee, LoanPenalty, LoanInsuranceFee, MinimumLimit, MaximumLimit, RequireGuarranters from LoanTypes t inner join dbo.InterestCalculationMethods m on m.MethodID = t.InterestCalculationMethodID  Where LoanTypeID = @LoanTypeID";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var loanType = dbs.Query<LoanType>(query, new { LoanTypeID = LoanTypeID }, commandType: CommandType.Text).FirstOrDefault();

                    return new ObjectResult(new { Data = loanType, StatusCode = HttpStatusCode.OK, Message = "Loan type fetched successfully..." });
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
        public IActionResult addLoanType(LoanType LoanType)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        //dbs.Open();
                        //SqlTransaction trans = dbs.BeginTransaction();
                        try
                        {
                            int rowsAffected = dbs.Query<int>("dbo.LoanTypesCreate",
                                   new
                                   {
                                       LoanName = LoanType.LoanName,
                                       InterestCalculationMethodID = LoanType.InterestCalculationMethodID,
                                       LoanInterest = LoanType.LoanInterest,
                                       LoanPenalty = LoanType.LoanPenalty,
                                       LoanInsuranceFee = LoanType.LoanInsuranceFee,
                                       ProcessingFee = LoanType.ProcessingFee,
                                       MinimumLimit = LoanType.MinimumLimit,
                                       MaximumLimit = LoanType.MaximumLimit,
                                       RequireGuarranters = LoanType.RequireGuarranters,
                                       CreatedBy = "SaccosApi"
                                   }, commandType: CommandType.StoredProcedure).FirstOrDefault();


                            //    trans.Commit();

                            return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "Loan type added successfully..." });
                        }
                        catch (Exception ex)
                        {
                            //trans.Rollback();
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
        public IActionResult UpdateLoanType(LoanType LoanType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        int rowsAffected = dbs.Query<int>("dbo.LoanTypesUpdate",
                                new
                                {
                                    LoanTypeID = LoanType.LoanTypeID,
                                    LoanName = LoanType.LoanName,
                                    LoanInterest = LoanType.LoanInterest,
                                    InterestCalculationMethodID = LoanType.InterestCalculationMethodID,
                                    LoanInsuranceFee = LoanType.LoanInsuranceFee,
                                    LoanPenalty = LoanType.LoanPenalty,
                                    ProcessingFee = LoanType.ProcessingFee,
                                    MinimumLimit = LoanType.MinimumLimit,
                                    MaximumLimit = LoanType.MaximumLimit,
                                    RequireGuarranters = LoanType.RequireGuarranters,
                                    LastModifiedBy = "SaccosApi"
                                }, commandType: CommandType.StoredProcedure).FirstOrDefault();


                        return new ObjectResult(new { Data = new { }, StatusCode = HttpStatusCode.OK, Message = "Loan type updated successfully..." });
                    }
                }
                else { return new ObjectResult(new {  StatusCode = HttpStatusCode.BadRequest }); }


            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.BadRequest, Message = errorMsg });
            }

        }


        [HttpDelete]
        [AllowAnonymous]
        public IActionResult RemoveLoanType(int LoanTypeID)
        {
            try
            {
                var query = @" Delete From dbo.LoanTypes Where LoanTypeID = @LoanTypeID";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                   dbs.Query(query, new { LoanTypeID = LoanTypeID }, commandType: CommandType.Text).FirstOrDefault();

                    return new ObjectResult(new {  StatusCode = HttpStatusCode.OK, Message = "Loan type removed successfully..." });
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
        public IActionResult GetLoanTypes(int pageNumber, int pageSize, string? searchTerm, string? sortColumn = "DateCreated", string? sortDirection = "DESC")
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
                        SELECT COUNT(*) FROM dbo.LoanTypes Where " + whereClause +
                        " Select LoanTypeID, LoanName, m.MethodName,LoanInterest, ProcessingFee, LoanInsuranceFee, LoanPenalty, MinimumLimit, MaximumLimit, RequireGuarranters FROM dbo.LoanTypes t inner join dbo.InterestCalculationMethods m on m.MethodID = t.InterestCalculationMethodID " +
                        " " +
                        "Order By LoanTypeID ASC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var reader = dbs.QueryMultiple(query, new { Skip = skip, Take = take });
                    int totalRecords = reader.Read<int>().FirstOrDefault();
                    List<LoanType> loanTypes = reader.Read<LoanType>().ToList();

                    //List<LoanType> loanTypes = dbs.Query<LoanType>("SELECT LoanTypeID, LoanName, Description,LoanInterest, ProcessingFee, LoanPenalty, MinimumLimit, MaximumLimit, RequiresGuarranters FROM dbo.LoanTypes", commandType: CommandType.Text).ToList();
                    var result = new PaginationResponse<dynamic>(totalRecords, loanTypes, pageNumber, pageSize);

                    return Ok(result);
                    //return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "Loan types fetched successfully..." });
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
        public IActionResult addLoanDuration(LoanDuration LoanDuration)
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
                            int rowsAffected = dbs.Query<int>("dbo.LoanDurationsCreate",
                                   new
                                   {
                                       LoanTypeID = LoanDuration.LoanTypeID,
                                       FromLoanAmount = LoanDuration.FromLoanAmount,
                                       ToLoanAmount = LoanDuration.ToLoanAmount,
                                       Duration = LoanDuration.Duration,
                                       CreatedBy = "SaccosApi"
                                   }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                            return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "Loan duration added successfully..." });
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
        public IActionResult UpdateLoanDuration(LoanDuration LoanDuration)
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
                            int rowsAffected = dbs.Query<int>("dbo.LoanDurationsUpdate",
                                   new
                                   {
                                       Id = LoanDuration.Id,
                                       LoanTypeID = LoanDuration.LoanTypeID,
                                       FromLoanAmount = LoanDuration.FromLoanAmount,
                                       ToLoanAmount = LoanDuration.ToLoanAmount,
                                       Duration = LoanDuration.Duration,
                                       LastModifiedBy = "SaccosApi"
                                   }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                            return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "Loan duration updated successfully..." });
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


        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetLoanDurations(int pageNumber, int pageSize, string? searchTerm, string? sortColumn = "DateCreated", string? sortDirection = "DESC")
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
                var query = @" SELECT COUNT(*) FROM dbo.LoanDurations Where " + whereClause +
                             " Select d.Id, d.LoanTypeID, t.LoanName, FromLoanAmount, ToLoanAmount, Duration FROM dbo.LoanDurations d  inner join dbo.LoanTypes t  on d.LoanTypeID = t.LoanTypeID Order By LoanTypeID ASC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var reader = dbs.QueryMultiple(query, new { Skip = skip, Take = take });
                    int totalRecords = reader.Read<int>().FirstOrDefault();
                    var loanDurations = reader.Read().ToList();

                    var result = new PaginationResponse<dynamic>(totalRecords, loanDurations, pageNumber, pageSize);

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
        public IActionResult GetLoanDuration(int Id)
        {
            try
            {
                var query = @" Select d.Id, t.LoanTypeID, t.LoanName, FromLoanAmount, ToLoanAmount, Duration from dbo.LoanDurations d inner join dbo.LoanTypes t  on d.LoanTypeID = t.LoanTypeID  Where Id = @Id";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var loanDuration = dbs.Query(query, new { Id = Id }, commandType: CommandType.Text).FirstOrDefault();

                    return new ObjectResult(new { Data = loanDuration, StatusCode = HttpStatusCode.OK, Message = "Loan duration fetched successfully..." });
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
