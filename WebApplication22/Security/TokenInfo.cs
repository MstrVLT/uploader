using System;
using Microsoft.AspNetCore.Authentication;

namespace WebDocLoader.Security
{
    public class TokenInfo 
    {
        public TokenInfo() {}

        public TokenInfo(AuthenticationTicket ticket, decimal isn, string fullname) 
        {
            CreatedAt = DateTime.Now;
            Ticket = ticket;
            ISN = isn;
            FullName = fullname;
        }
        
        public DateTime CreatedAt { get; set; }
        
        public AuthenticationTicket Ticket { get; set; }

        public decimal ISN { get; set; }
        public string FullName { get; set; }
    }
}