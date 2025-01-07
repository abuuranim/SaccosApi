using Dapper;
using Microsoft.Data.SqlClient;
using SaccosApi.DTO;
using System.Data;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace SaccosApi.Repository
{


   
    public class MemberRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;
        public MemberRepository(IConfiguration configuration)
        {
            _config = configuration;
            _connectionString = configuration["ConnectionStrings:DbContext"];
        }

        public async Task<MemberDetailsDTO> GetMemberDetailsAsync(Guid MemberID)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sql = "Select m.MemberID, u.Username, m.MembershipNo, m.FullName, m.EmailAddress from dbo.Members m Inner Join dbo.Users u on u.Email = m.EmailAddress and u.Id = @MemberID";
                return await db.QueryFirstOrDefaultAsync<MemberDetailsDTO>(sql, new { MemberID = MemberID });
            }
        }
    }
}
