using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebDocLoader.Oracle;
using WebDocLoader.Security;

namespace WebDocLoader.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticateController : ControllerBase
    {
        public AuthenticateController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            string cookieToken = Request.Cookies["token"];
            var authToken = _tokenService.ValidateToken(cookieToken);
            if (authToken != null)
                return Ok(_tokenService.GetFullNameByToken(cookieToken));
            else
                return Unauthorized();
        }

        [HttpPost]
        public IActionResult Post([FromForm] AuthenticateModel model)
        {
            string cookieToken = Request.Cookies["token"];
            var authToken = _tokenService.ValidateToken(cookieToken);
            if (authToken != null)
                return Ok(_tokenService.GetFullNameByToken(cookieToken));

            string token;
            var errorCode = _tokenService.Authenticate(model.Username, model.Password, out token);
            if (errorCode == 0)
            {
                HttpContext.Response.Cookies.Append("token", token);
                return Ok(_tokenService.GetFullNameByToken(token));
            }
            else
            {
                return Unauthorized(errorCode);
            }
        }

        private ITokenService _tokenService;
    }
}
