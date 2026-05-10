using Microsoft.AspNetCore.Authorization;
namespace MIO2.API.Authorization;



// This class represents the authorization rule itself.
// It does NOT contain logic.
// It simply defines the requirement:
// "Owner OR Admin can access the student resource."
public class StudentOwnerOrAdminOrTrainerRequirement : IAuthorizationRequirement
{
    
}