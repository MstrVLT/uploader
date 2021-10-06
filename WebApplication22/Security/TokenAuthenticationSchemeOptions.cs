using Microsoft.AspNetCore.Authentication;

namespace WebDocLoader.Security
{
    public class TokenAuthenticationSchemeOptions : AuthenticationSchemeOptions 
    {
        public const string Name = "TokenAuthenticationScheme";
    }
}