using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaccosApi.Models;
using System.Data;
using System.Net;
using Dapper;
using SaccosApi.DTO;
using Microsoft.Data.SqlClient;
using ASPNetCoreAuth.Models;
using System.Transactions;
using System.Data.Common;

namespace SaccosApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class MemberController : ControllerBase
    {
        private readonly IConfiguration _config;

        public MemberController(IConfiguration config)
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
        public IActionResult GetMemberStatistics()
        {
            try
            {

                var query = @"Select Status, count(*) [Count] from Members Where Status= 'Active' and IsApproved=1 group by Status";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var result = dbs.Query(query);

                    return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "members fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetMembershipStatus()
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
                        string query = "Select Top 1 m.FirstName, m.LastName, m.FullName, m.MembershipNo,  case m.IsApproved when 1 then 'Active' else 'Pending'  end  MembershipStatus From dbo.Users u Join dbo.Members m on m.EmailAddress = u.Email Where Username = @username";
                        var result = dbs.Query<dynamic>(query, new { Username = username }, commandType: CommandType.Text).FirstOrDefault();

                        return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "member details fetched successfully..." });
                    }
                }
                else {
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
        public IActionResult GetRelationships()
        {
            try
            {
     
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                   var relationships = dbs.Query<Relationship>("SELECT RelationshipID, RelationshipName FROM dbo.Relationships", commandType: CommandType.Text).ToList();

                    return new ObjectResult(new { Data = relationships, StatusCode = HttpStatusCode.OK, Message = "Relationships fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetApplicantDetails()
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

                        var memberDetails = dbs.Query<MemberDetails>("dbo.[MembersSelectByUserName]", new { Username = username }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                        if(memberDetails?.MembershipNo != null)
                        {
                            var heirDetails = dbs.Query<Heir>("dbo.[HeirsSelect]", new { MemberID = memberDetails.MemberID }, commandType: CommandType.StoredProcedure).ToList();
                            memberDetails.Heirs  = heirDetails;
                        }

                        return new ObjectResult(new { Data = memberDetails, StatusCode = HttpStatusCode.OK, Message = "User details fetched successfully..." });
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
        public IActionResult GetMemberDetailsByUserID()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim != null)
                {
                    var userID = userIdClaim.Value;
                    var heirsQuery = @"Select h.Id, h.FullName, h.MobileNo, r.relationshipId, r.RelationshipName from Heirs h inner join dbo.Relationships r on r.RelationshipID = h.RelationshipID Where MemberID =  @MemberID";

                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {

                        var memberDetails = dbs.Query<MemberDetails>("dbo.MembersSelectByUserID", new { UserID = userID }, commandType: CommandType.StoredProcedure).FirstOrDefault();
                        var heirDetails = dbs.Query<Heir>("dbo.HeirsSelectByUserID", new { UserID = userID }, commandType: CommandType.StoredProcedure).ToList();

                        if (memberDetails != null)
                        {
                            memberDetails.Heirs = heirDetails;
                        }

                        return new ObjectResult(new { Data = memberDetails, StatusCode = HttpStatusCode.OK, Message = "Member details fetched successfully..." });
                    }
                }
                else { return Unauthorized(); }

                
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetMemberDetails(Guid MemberID)
        {
            try
            {
                var query = @"SELECT [MemberID], [MembershipNo] ,[FullName], [FirstName], [MiddleName], [LastName], [Gender] ,[DateOfBirth], [MaritalStatus]
                            ,[EmailAddress] ,[MobileNo] ,[JobTitle] ,[Status] ,[PhysicalAddress] ,[NationalID] ,[CreatedBy] ,[DateCreated]
                            FROM [dbo].[Members] Where MemberID = @MemberID";


                var heirsQuery = @"Select h.Id, h.FullName, h.MobileNo, r.relationshipId, r.RelationshipName from Heirs h inner join dbo.Relationships r on r.RelationshipID = h.RelationshipID Where MemberID =  @MemberID";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var memberDetails = dbs.Query<MemberDetails>(query, new { MemberID = MemberID }, commandType: CommandType.Text).FirstOrDefault();
                    var heirDetails = dbs.Query<Heir>(heirsQuery, new { MemberID = MemberID }, commandType: CommandType.Text).ToList();

                    if (memberDetails != null)
                    {
                        memberDetails.Heirs = heirDetails;
                    }

                    return new ObjectResult(new { Data = memberDetails, StatusCode = HttpStatusCode.OK, Message = "Member details fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }


        [HttpGet]
        public IActionResult GetGuaranterPendingApprovalRequests(int pageNumber, int pageSize, string? searchTerm, string? sortColumn = "NotificationDate", string? sortDirection = "DESC")
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim != null)
                {
                    var userID = userIdClaim.Value;

                    int maximumPageSize = 100;
                    pageSize = pageSize < maximumPageSize ? pageSize : maximumPageSize;
                    int skip = (pageNumber - 1) * pageSize;
                    int take = pageSize;

                    string whereClause = "g.ApprovalStatus = 'Pending' And g.MembershipNo = @MembershipNo";

                    var query = @"
                        SELECT COUNT(*) FROM dbo.Notifications n Inner Join dbo.LoanGuarantors g on g.MembershipNo = n.MembershipNo AND g.LoanApplicationID = n.LoanApplicationID Where " + whereClause +
                            " Select n.LoanApplicationID, n.MembershipNo, Subject, Message, NotificationDate FROM dbo.Notifications n inner join dbo.LoanGuarantors g on g.MembershipNo = n.MembershipNo And g.LoanApplicationID = n.LoanApplicationID  Where " + whereClause +
                            " Order By " + sortColumn + " " + sortDirection + " OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        var memberDetails = dbs.Query<MemberDetails>("dbo.MembersSelectByUserID", new { UserID = userID }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                        var reader = dbs.QueryMultiple(query, new { MembershipNo = memberDetails?.MembershipNo, Skip = skip, Take = take });
                        int totalRecords = reader.Read<int>().FirstOrDefault();
                        var messages = reader.Read().ToList();


                        var result = new PaginationResponse<dynamic>(totalRecords, messages, pageNumber, pageSize);

                        return Ok(result);

                    }
                }
                else { return Unauthorized(); }

            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }


        [HttpGet]
        public IActionResult GetNewNotificationMessages()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim != null)
                {
                    var userID = userIdClaim.Value;
                    string connectionString = _config["ConnectionStrings:DbContext"];

                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
          
                        var memberDetails = dbs.Query<MemberDetails>("dbo.MembersSelectByUserID", new { UserID = userID }, commandType: CommandType.StoredProcedure).FirstOrDefault();
                        var messages = dbs.Query<dynamic>("Select LoanApplicationID, MembershipNo, Subject, Message, NotificationDate From Notifications  Where isRead = 0 and MembershipNo = @MembershipNo", new { MembershipNo = memberDetails?.MembershipNo }, commandType: CommandType.Text).ToList();

                        return new ObjectResult(new { Data = messages, StatusCode = HttpStatusCode.OK, Message = "Messages fetched successfully..." });
                    }
                }
                else { return Unauthorized(); }


            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpPost]
        public IActionResult CreateMember(MemberDetails MemberDetails)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {

                        var result = dbs.Query("dbo.MembersCreate",
                                new
                                {
                                    PFCheckNo = MemberDetails.PFCheckNo,
                                    FirstName = MemberDetails.FirstName,
                                    MiddleName = MemberDetails.MiddleName,
                                    LastName = MemberDetails.LastName,
                                    DateOfBirth = MemberDetails.DateOfBirth,
                                    Gender = MemberDetails.Gender,
                                    MaritalStatus = MemberDetails.MaritalStatus,
                                    NationalID = MemberDetails.NationalID,
                                    MobileNo = MemberDetails.MobileNo,
                                    EmailAddress = MemberDetails.EmailAddress,
                                    JobTitle = MemberDetails.JobTitle,
                                    PhysicalAddress = MemberDetails.PhysicalAddress,
                                    CreatedBy = "SaccosApi"
                                }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                        //if (result != null)
                        //{
              
                        //       dbs.Query("Update dbo.Users Set StageID = @StageID, LastModifiedBy = @LastModifiedBy, LastModified = @LastModified  Where Email = @Email",
                        //       new
                        //       {
                        //           StageID = 2,
                        //           Email = MemberDetails.EmailAddress,
                        //           LastModified = DateTime.Now,
                        //           LastModifiedBy = "SaccosApi"
                        //       }, commandType: CommandType.Text, transaction: trans).FirstOrDefault();
                            

                        //    trans.Commit();
                        //}invoi
                        return new ObjectResult(new { Data = result,  StatusCode = HttpStatusCode.OK, Message = "MemberDetails created successfully..." });

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



        [HttpPost]
        public IActionResult AddMember(MemberDetails MemberDetails)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        dbs.Open();
                        SqlTransaction trans = dbs.BeginTransaction();
           
                        var result = dbs.Query<dynamic>("dbo.MembersCreate",
                                new
                                {
                                    PFCheckNo = MemberDetails.PFCheckNo,
                                    FirstName = MemberDetails.FirstName,
                                    MiddleName = MemberDetails.MiddleName,
                                    LastName = MemberDetails.LastName,
                                    DateOfBirth = MemberDetails.DateOfBirth,
                                    Gender = MemberDetails.Gender,
                                    MaritalStatus = MemberDetails.MaritalStatus,
                                    NationalID = MemberDetails.NationalID,
                                    MobileNo = MemberDetails.MobileNo,
                                    EmailAddress = MemberDetails.EmailAddress,
                                    JobTitle = MemberDetails.JobTitle,
                                    PhysicalAddress = MemberDetails.PhysicalAddress,
                                    CreatedBy = "SaccosApi"
                                }, commandType: CommandType.StoredProcedure, transaction:trans).FirstOrDefault();

                        if (result != null)
                        {
                            foreach(var heir in MemberDetails.Heirs)
                            {
                                int rowsAffected = dbs.Query<int>("dbo.HeirsCreate",
                               new
                               {
                                   MemberID = result.MemberID,
                                   FullName = heir.FullName,
                                   MobileNo = heir.MobileNo,
                                   RelationshipId = heir.RelationshipId,
                                   CreatedBy = "SaccosApi"
                               }, commandType: CommandType.StoredProcedure, transaction: trans).FirstOrDefault();
                            }

                            trans.Commit();
                        }

                        return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "MemberDetails added successfully..." });
                        
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
        public IActionResult UpdateMember(MemberDetails MemberDetails)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        int affectedRows = dbs.Query<int>("dbo.MembersUpdate",
                                new
                                {
                                    MemberID = MemberDetails.MemberID,
                                    PFCheckNo = MemberDetails.PFCheckNo,
                                    FirstName = MemberDetails.FirstName,
                                    MiddleName = MemberDetails.MiddleName,
                                    LastName = MemberDetails.LastName,
                                    DateOfBirth = MemberDetails.DateOfBirth,
                                    Gender = MemberDetails.Gender,
                                    MaritalStatus = MemberDetails.MaritalStatus,
                                    NationalID = MemberDetails.NationalID,
                                    MobileNo = MemberDetails.MobileNo,
                                    EmailAddress = MemberDetails.EmailAddress,
                                    JobTitle = MemberDetails.JobTitle,
                                    PhysicalAddress = MemberDetails.PhysicalAddress,
                                    LastModifiedBy = "SaccosApi"
                                }, commandType: CommandType.StoredProcedure).FirstOrDefault();

   
                        return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "MemberDetails updated successfully..." });

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



        [HttpPost]
        public IActionResult AddHeir(Heir HeirDetails)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {

                        var heir = dbs.Query<Heir>("dbo.HeirsCreate",
                                new
                                {
                                    MemberID = HeirDetails.MemberID,
                                    FullName = HeirDetails.FullName,
                                    MobileNo = HeirDetails.MobileNo,
                                    RelationshipID = HeirDetails.RelationshipId,
                                    CreatedBy = "SaccosApi"
                                }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                       

                        return new ObjectResult(new { Data = heir, StatusCode = HttpStatusCode.OK, Message = "Heir details added successfully..." });

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


        [HttpPost]
        public IActionResult CreateHeirByUserID(HeirInformation HeirDetails)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    string connectionString = _config["ConnectionStrings:DbContext"];
                    var userIdClaim = User.FindFirst("userId");
                    if (userIdClaim != null)
                    {
                        var userID = userIdClaim.Value;
                        using (SqlConnection dbs = new SqlConnection(connectionString))
                        {

                            var heir = dbs.Query<Heir>("dbo.HeirsCreateByUserID",
                                    new
                                    {
                                        UserID = userID,
                                        FullName = HeirDetails.FullName,
                                        MobileNo = HeirDetails.MobileNo,
                                        RelationshipID = HeirDetails.RelationshipID,
                                        CreatedBy = "SaccosApi"
                                    }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                            return new ObjectResult(new { Data = heir, StatusCode = HttpStatusCode.OK, Message = "Heir details added successfully..." });

                        }
                    }
                    else return Unauthorized();
                
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
        public IActionResult ActiveMembers()
        {
            try
            {
                
                var query = @"Select MembershipNo, FullName,EmailAddress,MobileNo from dbo.Members Where Status = 'Active'";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    var result = dbs.Query(query);

                    return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "members fetched successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult MemberDetailsByUserID()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim != null)
                {
                    var userID = userIdClaim.Value;

                    var query = @"Select MembershipNo, u.FullName,EmailAddress,MobileNo from dbo.Members m Join dbo.Users u on m.EmailAddress = u.Email Where m.Status = 'Active' And u.Id = @userID";

                    string connectionString = _config["ConnectionStrings:DbContext"];
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        var result = dbs.Query<dynamic>(query, new { userID = userID }, commandType: CommandType.Text).FirstOrDefault();

                        return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "member details fetched successfully..." });
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
        public IActionResult GetActiveMembers(int pageNumber, int pageSize, string? searchTerm, string? sortColumn = "DateCreated", string? sortDirection = "DESC")
        {
            try
            {
                int maximumPageSize = 100;
                pageSize = pageSize < maximumPageSize ? pageSize : maximumPageSize;
                int skip = (pageNumber - 1) * pageSize;
                int take = pageSize;

                string whereClause = "IsApproved = 1 And Status = 'Active' ";
                if (searchTerm != null)
                {
                    whereClause = whereClause + " AND MembershipNo LIKE '%" + searchTerm + "%'"
                        + " OR (FullName  LIKE '%" + searchTerm + "%') AND " + whereClause;

                }
                var query = @"
                        SELECT COUNT(*) FROM dbo.Members Where " + whereClause +
                        " Select [MemberID] ,[MembershipNo], [FullName], [Gender], [DateOfBirth], [MaritalStatus], [EmailAddress], [MobileNo], [JobTitle] , [Status], [PhysicalAddress], [NationalID], [IsApproved] ,[CreatedBy] FROM dbo.Members Where " + whereClause +
                        " Order By DateCreated ASC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var reader = dbs.QueryMultiple(query, new { Skip = skip, Take = take });
                    int totalRecords = reader.Read<int>().FirstOrDefault();
                    //List<MemberDetails> members = reader.Read<MemberDetails>().ToList();
                    var members = reader.Read().ToList();


                    var result = new PaginationResponse<dynamic>(totalRecords, members, pageNumber, pageSize);

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
        public IActionResult GetPendingApplications(int pageNumber, int pageSize, string? searchTerm, string? sortColumn = "DateCreated", string? sortDirection = "DESC")
        {
            try
            {
                int maximumPageSize = 100;
                pageSize = pageSize < maximumPageSize ? pageSize : maximumPageSize;
                int skip = (pageNumber - 1) * pageSize;
                int take = pageSize;

                string whereClause = "IsApproved = 0";
                if (searchTerm != null)
                {
                    whereClause = whereClause + " AND MembershipNo LIKE '%" + searchTerm + "%'"
                        + " OR (FullName  LIKE '%" + searchTerm + "%') AND " + whereClause;

                }
                var query = @"
                        SELECT COUNT(*) FROM dbo.Members Where " + whereClause +
                        " Select [MemberID] ,[MembershipNo], [FullName], [Gender], [DateOfBirth], [MaritalStatus], [EmailAddress], [MobileNo], [JobTitle] , [NationalID], case IsApproved when 1 then 'Active' else 'Pending'  end  MembershipStatus FROM dbo.Members Where " + whereClause + 
                        "Order By DateCreated ASC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var reader = dbs.QueryMultiple(query, new { Skip = skip, Take = take });
                    int totalRecords = reader.Read<int>().FirstOrDefault();
                    //List<MemberDetails> members = reader.Read<MemberDetails>().ToList();
                    var members = reader.Read().ToList();


                    var result = new PaginationResponse<dynamic>(totalRecords, members, pageNumber, pageSize);

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
        public IActionResult GetUnUsedOTPs(int pageNumber, int pageSize, string? searchTerm, string? sortColumn = "CreatedAt", string? sortDirection = "DESC")
        {
            try
            {
                int maximumPageSize = 100;
                pageSize = pageSize < maximumPageSize ? pageSize : maximumPageSize;
                int skip = (pageNumber - 1) * pageSize;
                int take = pageSize;

                string whereClause = "ExpiresAt > GetDate() ";
                if (searchTerm != null)
                {
                    whereClause = whereClause + " AND Username LIKE '%" + searchTerm + "%'";
                }
                var query = @"
                        SELECT COUNT(*) FROM OTPs  Where " + whereClause +
                        " Select OTP, Username, CreatedAt, ExpiresAt From OTPs  Where " + whereClause +
                        "Order By CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {

                    var reader = dbs.QueryMultiple(query, new { Skip = skip, Take = take });
                    int totalRecords = reader.Read<int>().FirstOrDefault();
                    var otps = reader.Read().ToList();
                    var result = new PaginationResponse<dynamic>(totalRecords, otps, pageNumber, pageSize);

                    return Ok(result);
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }





        [HttpDelete]
        public IActionResult RemoveHeir(int Id)
        {
            try
            {
                var query = @"Delete From dbo.Heirs Where Id = @Id";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    dbs.Query(query, new { Id = Id }, commandType: CommandType.Text).FirstOrDefault();

                    return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "Heir removed successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpDelete]
        public IActionResult RemoveOTP(int otp)
        {
            try
            {
                var query = @"Delete From dbo.OTPS Where OTP = @OTP";

                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    dbs.Query(query, new { OTP = otp}, commandType: CommandType.Text).FirstOrDefault();

                    return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "OTP removed successfully..." });
                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }

        [HttpGet]
        public IActionResult GetApplicantHeirInformation()
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];

                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim != null)
                {
                    //var userId = Guid.Parse(userIdClaim.Value);
                    var userId = userIdClaim.Value;
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        var heirs = dbs.Query<Heir>("dbo.HeirsSelectByUserID", new { UserID = userId }, commandType: CommandType.StoredProcedure).ToList();
                        return new ObjectResult(new { Data = heirs, StatusCode = HttpStatusCode.OK, Message = "Heir details fetched successfully..." });
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
        public IActionResult GetHeirInformation(Guid memberID)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];

                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim != null)
                {

                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        var heirs = dbs.Query<Heir>("dbo.HeirsSelectByMemberID", new { MemberID = memberID }, commandType: CommandType.StoredProcedure).ToList();
                        return new ObjectResult(new { Data = heirs, StatusCode = HttpStatusCode.OK, Message = "Heir details fetched successfully..." });
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
        public IActionResult IsHeirInformationCompleted()
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim != null)
                {
                    var userID = userIdClaim.Value;
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        var result = dbs.Query<dynamic>("dbo.HeirsCheckForInformationCompleteness", new { UserID = userID }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                        return new ObjectResult(new { Data = result, StatusCode = HttpStatusCode.OK, Message = "success..." });
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
        public IActionResult ApproveMembershipApplication(string MembershipNo)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];

                var userIdClaim = User.FindFirst("username");
                if (userIdClaim != null)
                {
                    var username = userIdClaim.Value;
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {
                        dbs.Open();
                        SqlTransaction trans = dbs.BeginTransaction();
                     
                        try
                        {

                            dbs.Query("Update [dbo].[Members]  SET [IsApproved] = 1, LastModifiedBy = @LastModifiedBy, LastModified = GetDate() Where MembershipNo = @MembershipNo", new { MembershipNo = MembershipNo, LastModifiedBy = username }, commandType: CommandType.Text, transaction: trans).FirstOrDefault();

                            var accounts = dbs.Query<dynamic>("Select AccountTypeID, AccountTypeName  From dbo.AccountTypes  WHERE Category IN('SHARE', 'SAVINGS')", commandType: CommandType.Text, transaction: trans).ToList();
                            if(accounts != null)
                            {
                                foreach (dynamic account in accounts)
                                {
                                    dbs.Query("dbo.AccountsCreate",
                                                                new
                                                                {
                                                                    MembershipNo = MembershipNo,
                                                                    AccountTypeID = account.AccountTypeID,
                                                                    CreatedBy = username
                                                                }, commandType: CommandType.StoredProcedure, transaction:trans).FirstOrDefault();
                                }
                            }

                            trans.Commit();

                            return new ObjectResult(new { StatusCode = HttpStatusCode.OK, Message = "Membership application has been approved successfully..." });
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            //throw ;
                            throw new Exception(ex.GetBaseException().Message);
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


    }


}
