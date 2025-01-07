
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SaccosApi.Repository;
using SaccosApi.Services;
using System.Text;

namespace ASPNetCoreAuth
{
    public class Startup
    {
        private readonly IConfiguration _config;
        public Startup(IConfiguration configuration)
        {
            _config = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder =>
                    {
                        builder.WithOrigins("http://example.com") 
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    });
            });

             //Configure Identity services and middleware to implement Authentication and Authorization
             services.AddSingleton<UserRepository>();
             services.AddSingleton<AuthService>();
             services.AddSingleton<LoanService>();
            services.AddSingleton<LoanRepository>();
            services.AddSingleton<MemberRepository>();
             services.AddSingleton<MemberService>();

            // Add other services
            services.AddSingleton<EmailNotificationService>();
            services.AddSingleton<SecureOtpGenerator>();

            // Configure JWT authentication
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Not recommended for production
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    //Token validation parameters (e.g., issuer, audience, key)
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
            
                    /*
                     *  ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "your_issuer",
                        ValidAudience = "your_audience",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your_secret_key"))
                     * 
                     */
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        context.Response.Headers.Add("StatusCode", "401");
                        var response = new
                        {
                            StatusCode = context.Response.StatusCode,
                            Message = "Custom 401 Unauthorized: Access is denied due to invalid credentials."
                        };
                        return context.Response.WriteAsJsonAsync(response);
                    }
                };
            });

            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("AdminRole", policy =>
            //        policy.RequireRole("Admin"));

            //    options.AddPolicy("RequireAdminOrModeratorRole", policy =>
            //        policy.RequireRole("Admin", "Moderator"));

            //    options.AddPolicy("RequireCustomClaim", policy =>
            //        policy.RequireClaim("CustomClaimType"));

            //    options.AddPolicy("RequireMinimumAge", policy =>
            //        policy.Requirements.Add(new MinimumAgeRequirement(18)));
            //});

          



            services.AddControllers();
            services.AddCors(options =>
            {
                options.AddPolicy("default", policy =>
                {
                    policy  //WithOrigins("http://localhost:5253", "http://example.com", "http://another-example.com") // specify the allowed origins
                           .AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            
            // Use authentication middleware
            app.UseAuthentication();


            // Use routing
            app.UseRouting(); 

            app.UseAuthorization();


            app.UseCors("default"); 

            // Use endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}
