using Microsoft.AspNetCore.Authentication;

namespace WebDocLoader.Security
{
    public interface ITokenService 
    {
        string GetFullNameByToken(string token);
        int Authenticate(string user, string password, out string token);
        AuthenticationTicket ValidateToken(string token);
    }
}