using ASPNetCoreAuth.Models;
using Microsoft.IdentityModel.Tokens;
using SaccosApi.Repository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SaccosApi.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(UserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<string> AuthenticateAsync(string username, string password)
        {
            if (!await _userRepository.CheckPasswordAsync(username, password))
            {
                return null;
            }

            var user = await _userRepository.GetUserByUsernameAsync(username);
            var roles = await _userRepository.GetUserRolesAsync(user.Id);

            var claims = new List<Claim>
            {
                new Claim("username", username),
                new Claim("fullName", user.FullName),
                new Claim("email", user.Email),
                new Claim("stage", user.StageName),
                new Claim("userID", user.Id.ToString()),
            };

              claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            //claims.AddRange(roles.Select(role => new Claim(ClaimsIdentity.DefaultRoleClaimType, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(120), //60
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
