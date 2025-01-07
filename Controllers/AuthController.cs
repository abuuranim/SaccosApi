using ASPNetCoreAuth.Models;
using ASPNetCoreAuth.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Dapper;
using System.Security.Claims;
using System.Text;
using System.Data;
using System.Net;
using Microsoft.AspNetCore.Cors;
using SaccosApi.Models;
using Microsoft.Data.SqlClient;
using SaccosApi.Services;
using System.Reflection;
using Azure.Core;
using SaccosApi.DTO;
using System.Text.RegularExpressions;

namespace ASPNetCoreAuth.Controllers
{

    [ApiController]
    //[EnableCors("default")]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AuthService _authService;
        private readonly EmailNotificationService _emailService;
        private readonly SecureOtpGenerator _secureOtpGenerator;

        public AuthController(IConfiguration config, AuthService authService, EmailNotificationService emailService, SecureOtpGenerator otpGenerator)
        {
            _config = config;
            _authService = authService;
            _emailService = emailService;
            _secureOtpGenerator = otpGenerator;
        }

        [HttpGet]
        [AllowAnonymous]
        public string Init()
        {
            //return ValidatePassword("Password123");
            return "Auth Controller Service started successfully... ";

        }


        [HttpGet]
        [Authorize]
        public IActionResult Test()
        {

            // Get the user claims

            var userIdClaim = User.FindFirst("username")?.Value; // Standard claim type for user ID in JWT
            var userId = User.FindFirst("userId")?.Value; // Standard claim type for user ID in JWT
            return Ok("Valid token!");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        //[Authorize(Policy = "AdminRole")]
        public IActionResult AdminTest()
        {
            var iss = User.FindFirst("iss")?.Value; // Standard claim type for user ID in JWT
            return Ok("Valid token!");
        }




        [HttpPost]
        [AllowAnonymous]
        public IActionResult Register(AuthUser AuthUser)
        {
            try
            {
                if (ValidatePassword(AuthUser?.Password) == false) {
                    return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.BadRequest, Message = "Password should contains at least one uppercase letter and one number and one non-alphanumeric character" });
                }


                string saltingKey = _config["Settings:SaltingKey"];
                var hashedPassword = Helper.GeneratePasswordHash(AuthUser.Password, saltingKey);
                string connectionString = _config["ConnectionStrings:DbContext"];
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                    dbs.Open();
                    SqlTransaction trans = dbs.BeginTransaction();
                    try
                    {
                       dbs.Query<int>("dbo.UsersCreate",
                               new
                               {
                                   AuthUser.Username,
                                   Password = hashedPassword,
                                   FirstName = AuthUser.FirstName,
                                   MiddleName = AuthUser.MiddleName,
                                   LastName = AuthUser.LastName,
                                   Email = AuthUser.Email,
                                   CreatedBy = "SaccosApi"
                               }, commandType: CommandType.StoredProcedure, transaction: trans).FirstOrDefault();



                        var OTP = _secureOtpGenerator.GenerateSecureOtp();

                        dbs.Query<int>("dbo.OTPsCreate",
                          new
                          {
                              OTP= OTP,
                              Username = AuthUser.Username,
                          }, commandType: CommandType.StoredProcedure, transaction: trans).FirstOrDefault();

                        trans.Commit();

                        var smtpServer = _config["EmailSettings:SmtpServer"];
                        var smtpPort = _config["EmailSettings:SmtpPort"];
                        var senderEmail = _config["EmailSettings:SenderEmail"];
                        var senderName = _config["EmailSettings:SenderName"];
                        var username = _config["EmailSettings:Username"];
                        var password = _config["EmailSettings:Password"];
                        var recipientEmail = AuthUser.Email;
                        var subject = "Your One-Time Password (OTP) Code";
                        var body = $"<p>Dear {AuthUser.FirstName} ,</p>" +
                                   $"<p>Your OTP code is: {OTP}</p>" +
                                   $"<p>Please use this code to complete your registration process. This code will expire in 30 minutes.</p>" +
                                   $"<p>Thank you,</p>";

                        _emailService.SendEmail(smtpServer, int.Parse(smtpPort), senderEmail, senderName, username, password, recipientEmail, subject, body);


                        return new ObjectResult(new { Data = new { }, StatusCode = HttpStatusCode.OK, Message = "Registered Successfully..." });

                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        string errorMsg = ex.GetBaseException().Message;
                        throw new Exception(errorMsg);
                    }



                }
            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.BadRequest, Message = errorMsg });
            }

        }

        private bool ValidatePassword(string password)
        {

            // Regex pattern to check for at least one letter, one number, and one non-alphanumeric character
            //string pattern = @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z\d]).+$";
            string pattern = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z\d]).+$";

            bool isMatched = Regex.IsMatch(password, pattern);

            return isMatched;
        }


        [AllowAnonymous]
        [HttpPost]

        public async Task<IActionResult> Login([FromBody] AuthUser authUser)
        {
           
            var access_token = await _authService.AuthenticateAsync(authUser.Username, authUser.Password);
            if (access_token == null)
            {
                return BadRequest(new { message = "Invalid username or password" });
            }

            return Ok(new { access_token });

            /*
  
            string connectionString = _config["ConnectionStrings:DbContext"];

            if (authUser.Username == null || authUser.Password == null)
            {
                return Unauthorized();
                
            }
            string saltingKey = _config["Settings:SaltingKey"];

            var hashedPassword = Helper.GeneratePasswordHash(authUser.Password, saltingKey);

            using (SqlConnection dbs = new SqlConnection(connectionString))
            {
                try
                {
                    AuthUser? user = dbs.Query<AuthUser>("Select Username, Password From dbo.Users Where username = @Username", new { Username = authUser.Username}, commandType: CommandType.Text).FirstOrDefault();

                    if(user != null && string.Equals(user.Username, authUser.Username, StringComparison.OrdinalIgnoreCase) && hashedPassword == user.Password) {

                        var access_token = GenerateToken(authUser.Username);
                        return Ok(new { access_token });
                    }
                    return Unauthorized("Invalid username or password");

                }
                catch (Exception exception)
                {
                    string errorMsg = exception.GetBaseException().Message;
                    return new BadRequestObjectResult(new { StatusCode = HttpStatusCode.BadRequest, Message = errorMsg });
                }

                finally
                {
                    dbs.Close();
                }

            }
            */

        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult ValidateOTP(OTPValidation OtpValidation)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];

  
                    var isValid = false;
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {

                        var _otp = dbs.Query<int>("dbo.OTPsValidation", new { Username = OtpValidation.Username, OTP = OtpValidation.OTP }, commandType: CommandType.StoredProcedure).FirstOrDefault();
                        if(_otp == OtpValidation.OTP)
                        {
                            isValid = true;
                        }
                        
                        return new ObjectResult(new { Data = new {IsValid = isValid }, StatusCode = HttpStatusCode.OK, Message = "OTP validation staus..." });

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
        public IActionResult ResendOTP(ResendOTP Otp)
        {
            try
            {
                string connectionString = _config["ConnectionStrings:DbContext"];

                var OTP = _secureOtpGenerator.GenerateSecureOtp();
                using (SqlConnection dbs = new SqlConnection(connectionString))
                {
                  dbs.Query<int>("dbo.OTPsCreate",
                  new
                  {
                      OTP = OTP,
                      Username = Otp.Username,
                  }, commandType: CommandType.StoredProcedure).FirstOrDefault();


                }

                var smtpServer = _config["EmailSettings:SmtpServer"];
                var smtpPort = _config["EmailSettings:SmtpPort"];
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var senderName = _config["EmailSettings:SenderName"];
                var username = _config["EmailSettings:Username"];
                var password = _config["EmailSettings:Password"];
                var recipientEmail = Otp.Email;
                var subject = "Your One-Time Password (OTP) Code";
                var body = $"<p>Dear {Otp.Name} ,</p>" +
                           $"<p>Your OTP code is: {OTP}</p>" +
                           $"<p>Please use this code to complete your registration process. This code will expire in 30 minutes.</p>" +
                           $"<p>Thank you,</p>";

                _emailService.SendEmail(smtpServer, int.Parse(smtpPort), senderEmail, senderName, username, password, recipientEmail, subject, body);

                return new ObjectResult(new { Data = "Success", StatusCode = HttpStatusCode.OK, Message = "OTP generated successfully..." });

            }
            catch (Exception exception)
            {
                string errorMsg = exception.GetBaseException().Message;
                return new BadRequestObjectResult(new { HttpStatusCode = HttpStatusCode.InternalServerError, Message = errorMsg });
            }

        }




        [HttpGet]
        public IActionResult GetAccountDetails()
        {
            try
            {

                var query = @"Select Id, Username, FirstName, MiddleName, LastName, Email, IsActive, StageID from dbo.Users Where Username = @Username";

                string connectionString = _config["ConnectionStrings:DbContext"];
                var usernameClaim = User.FindFirst("username");
                if (usernameClaim != null)
                {
                    var username = usernameClaim.Value;
                    using (SqlConnection dbs = new SqlConnection(connectionString))
                    {

                        var userInfo = dbs.Query<AuthUser>(query, new { Username = username }, commandType: CommandType.Text).FirstOrDefault();

                        return new ObjectResult(new { Data = userInfo, StatusCode = HttpStatusCode.OK, Message = "User details fetched successfully..." });
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
        public IActionResult UserDeclaration()
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
                        var result = dbs.Query("dbo.UsersDeclaration", new { UserID = userID }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                        return new ObjectResult(new { Data = result,  StatusCode = HttpStatusCode.OK, Message = "success..." });
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
    }
}
