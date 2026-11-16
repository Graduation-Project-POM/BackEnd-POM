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

        // Random password for accounts created without one. Cryptographically
        // random (RandomNumberGenerator), 16 characters, and always contains an
        // upper-case letter, a lower-case letter, a digit and a symbol, so it
        // satisfies the password rules (min. 8 characters) with room to spare.
        // Look-alike characters (O/0, l/1/I) are left out so it can be read
        // aloud or copied by hand.
        public static string GenerateRandomPassword(int length = 16)
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "!@#$%^&*-_=+?";
            const string all = upper + lower + digits + symbols;

            var chars = new char[length];
            chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            chars[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
            for (var i = 4; i < length; i++)
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

            // Fisher–Yates shuffle so the guaranteed classes aren't always first.
            for (var i = length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
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
