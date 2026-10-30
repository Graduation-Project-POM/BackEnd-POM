using System.ComponentModel.DataAnnotations;
using MIO.Core.Enums;

namespace MIO.Core.DTOs.UserDtos
{
    // Admin-only edit (PUT /User/{id}). Kept separate from UpdateUserDTO so
    // users can't change their own role, status or password via PUT /User/me.
    // Null fields are left unchanged.
    public class AdminUpdateUserDTO : UpdateUserDTO
    {
            [MinLength(8)]
            public string? NewPassword { get; set; }

            [Range(1, 10)]
            public Role? RoleID { get; set; }

            public bool? IsActive { get; set; }
    }
}
