using SaccosApi.Repository;
using System.Security.Cryptography;

namespace SaccosApi.Services
{
    public class SecureOtpGenerator
    {
        private readonly UserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public SecureOtpGenerator(UserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }
        public int GenerateSecureOtp()
        {
            Random random = new Random();
            return random.Next(10000, 100000);
        }
    }
}
