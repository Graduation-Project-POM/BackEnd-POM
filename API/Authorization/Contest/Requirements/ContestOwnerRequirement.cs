using Microsoft.AspNetCore.Authorization;
namespace MIO2.API.Authorization.Contest;



// This class represents the authorization rule itself.
// It does NOT contain logic.
// It simply defines the requirement:
// "Owner (the Trainer who created the contest) OR Admin can access the contest resource."
public class 
    ContestOwnerRequirement : IAuthorizationRequirement
{

}