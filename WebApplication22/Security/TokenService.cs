using System.Buffers.Text;
using System.Text;
using System.Security.Cryptography;
using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using WebDocLoader.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace WebDocLoader.Security
{
    public class TokenService : ITokenService 
    {
        public TokenService(IConfiguration configuration, IServiceProvider provider, OracleConnection conn) 
        {
            _configuration = configuration;
            _provider = provider;
            _conn = conn;
        }

        public int Authenticate(string username, string password, out string token) 
        {
            User user = ValidateCredentials(username, password, out var errorCode);
            if (user != null) 
            {
                token = Guid.NewGuid().ToString();
                AuthenticationTicket ticket = CreateAuthenticationTicket(user.ISN);

                _conn.Open();
                try
                {
                    using var transaction = _conn.BeginTransaction();
                    var fullName = UserSEC.GetUserFIO(transaction, user.ISN);
                    _tokens.TryAdd(token, new TokenInfo(ticket, user.ISN, fullName));
                    return errorCode;
                }
                finally
                {
                    _conn.Close();
                }
            }
            else 
            {
                token = null;
                return errorCode;
            }
        }


        public string GetFullNameByToken(string token)
        {
            if (token == null)
                return null;

            TokenInfo tokenInfo = null;
            if (_tokens.TryGetValue(token, out tokenInfo))
            {
                return tokenInfo.FullName;
            }
            else
            {
                return null;
            }
        }

        public AuthenticationTicket ValidateToken(string token) 
        {
            if(token == null)
                return null;

            TokenInfo tokenInfo = null;
            if (_tokens.TryGetValue(token, out tokenInfo)) 
            {
                return tokenInfo.Ticket;
            }
            else 
            {
                return null;
            }
        }

        private AuthenticationTicket CreateAuthenticationTicket(decimal isn) 
        {
            Claim[] claims = new Claim[] 
            {
                new Claim(ClaimTypes.NameIdentifier, isn.ToString())
            };        
            
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, nameof(TokenAuthenticationHandler));
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            AuthenticationTicket authTicket = new AuthenticationTicket(claimsPrincipal, TokenAuthenticationSchemeOptions.Name);

            return authTicket;
        }

        private User ValidateCredentials(string username, string password, out int errorCode)
        {
            MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(password));
            byte[] hashBytes = _cryptoProvider.ComputeHash(stream);
            string hashString = System.Convert.ToBase64String(hashBytes);

            _conn.Open();
            try
            {
                using var transaction = _conn.BeginTransaction();
                errorCode = UserSEC.SetUser(transaction, username, password, out var userISN);

                if (!userISN.HasValue)
                    return default(User);
                else
                    return new User { ISN = userISN.Value };
            }
            finally
            {
                _conn.Close();
            }
        }

        private ConcurrentDictionary<string, TokenInfo> _tokens = new ConcurrentDictionary<string, TokenInfo>(); 
        private IConfiguration _configuration;
        private IServiceProvider _provider;
        private OracleConnection _conn;
        SHA256 _cryptoProvider = SHA256CryptoServiceProvider.Create();
            
        
    }
}