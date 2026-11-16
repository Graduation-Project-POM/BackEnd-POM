using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MIO.Infrastructure.Services
{
    public static class PhoneNumbers
    {
        // Name of the filtered unique index on Users.PhoneNumber (see UserConfiguration).
        public const string UniqueIndexName = "IX_Users_PhoneNumber";

        // Canonical stored form, so "059-586 0351" and "0595860351" are the same
        // number for the uniqueness rule. Keeps digits and a leading '+', drops
        // the separators people type (spaces, dashes, dots, brackets). Blank → null.
        public static string? Normalize(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            var trimmed = phone.Trim();
            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            if (digits.Length == 0)
                return null;

            return trimmed.StartsWith('+') ? "+" + digits : digits;
        }

        // True when a save failed because another account already holds the
        // phone number — i.e. two requests raced past the pre-check and the
        // database's unique index rejected the second one.
        public static bool IsDuplicatePhone(DbUpdateException ex) =>
            ex.InnerException is SqlException { Number: 2601 or 2627 } sql &&
            sql.Message.Contains(UniqueIndexName, StringComparison.OrdinalIgnoreCase);
    }
}
