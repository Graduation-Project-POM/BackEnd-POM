using MIO.Core.Entities;

namespace MIO.Core.DTOs.UserDtos
{
    // A newly created account, plus the password the server generated for it
    // when none was supplied (null otherwise). The generated password is only
    // ever returned here, once; the database stores just its hash.
    public class CreatedUserResult
    {
        public required User User { get; init; }
        public string? GeneratedPassword { get; init; }
    }
}
