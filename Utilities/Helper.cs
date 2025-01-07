using System.Security.Cryptography;
using System.Text;

namespace ASPNetCoreAuth.Utilities
{
    public static  class Helper
    {
  

        public static string GeneratePasswordHash(string password, string salt)
        {
            // Convert password and salt to byte arrays
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] saltBytes = Convert.FromBase64String(salt);

            // Combine password and salt
            byte[] combinedBytes = new byte[passwordBytes.Length + saltBytes.Length];
            Array.Copy(passwordBytes, 0, combinedBytes, 0, passwordBytes.Length);
            Array.Copy(saltBytes, 0, combinedBytes, passwordBytes.Length, saltBytes.Length);

            // Compute the hash
            using (var sha256 = new HMACSHA256(Encoding.UTF8.GetBytes("your_secret_key_here")))
            {
                byte[] hashBytes = sha256.ComputeHash(combinedBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public static DateTime GetLastDayOfMonth(DateTime date)
        {
            int year = date.Year;
            int month = date.Month;
            int lastDay = DateTime.DaysInMonth(year, month);

            return new DateTime(year, month, lastDay);
        }
    }
}
