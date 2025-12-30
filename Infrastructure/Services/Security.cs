using System.Security.Cryptography;
using System.Text;

namespace MIO.Infrastructure.Services
{
    public static class Security
    {

        public static void CreatePasswordHash(string password, out byte[] passwordHash,
            out byte[] passwordsalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordsalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        public static bool VerifyPasswordHash(string password,  byte[] passwordHash,
            byte[] passwordsalt)
        {
            using (var hmac = new HMACSHA512(passwordsalt))
            {
               
                var ComputedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                return ComputedHash.SequenceEqual(passwordHash);

            }
        }
    }
}
