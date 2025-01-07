using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;
using ASPNetCoreAuth.Models;
using ASPNetCoreAuth.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using SaccosApi.DTO;

namespace SaccosApi.Repository
{
    public class UserRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config; 

        public UserRepository(IConfiguration configuration)
        {
            //_connectionString = configuration.GetConnectionString("DefaultConnection");
            _config = configuration;
            _connectionString = configuration["ConnectionStrings:DbContext"];
        }
        public async Task<AuthUser> GetUserByUsernameAsync(string username)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = "SELECT u.*, a.Alias as StageName FROM dbo.Users u inner join dbo.ApplicationStages a on a.StageID = u.StageID WHERE Username = @Username";
                return await db.QueryFirstOrDefaultAsync<AuthUser>(sql, new { Username = username });
            }
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = @"
                SELECT r.Name 
                FROM Roles r
                INNER JOIN UserRoles ur ON r.Id = ur.RoleId
                WHERE ur.UserId = @UserId";
                return await db.QueryAsync<string>(sql, new { UserId = userId });
            }
        }

        public async Task<bool> CheckPasswordAsync(string username, string password)
        {
            var user = await GetUserByUsernameAsync(username);
            if (user == null)
            {
                return false;
            }

            // Here you would typically hash the input password and compare it with the stored hash
            string saltingKey = _config["Settings:SaltingKey"];
            var hashedPassword = Helper.GeneratePasswordHash(password, saltingKey);

            return user.Password == hashedPassword;
        }


    }
}
