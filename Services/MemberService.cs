using Microsoft.IdentityModel.Tokens;
using SaccosApi.DTO;
using SaccosApi.Repository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SaccosApi.Services
{
    public class MemberService
    {
        private readonly MemberRepository _memberRepository;
        private readonly IConfiguration _configuration;

        public MemberService(MemberRepository memberRepository, IConfiguration configuration)
        {
            _memberRepository = memberRepository;
            _configuration = configuration;
        }

        public async Task<MemberDetailsDTO> GetMemberDetailsAsync(Guid MemberID)
        {
  
            var memberDetails = await _memberRepository.GetMemberDetailsAsync(MemberID);

            return memberDetails;
        }
    }
}
