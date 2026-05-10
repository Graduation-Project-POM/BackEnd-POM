using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
namespace MIO.Infrastructure.Services
{
    public static class Security
    {

        
        public static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            
            salt = RandomNumberGenerator.GetBytes(16);

            
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = 4, // CPU threads
                MemorySize = 65536,      // 64 MB
                Iterations = 4           // time cost
            };

            hash = argon2.GetBytes(32); // 256-bit hash
        }

        public static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = storedSalt,
                DegreeOfParallelism = 4,
                MemorySize = 65536,
                Iterations = 4
            };

            var computedHash = argon2.GetBytes(32);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
        
        
        public static string HashRefreshToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
        
        //old approach
        /*public static void CreatePasswordHash(string password, out byte[] passwordHash,
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
        }*/
        
        
        
    }
}
