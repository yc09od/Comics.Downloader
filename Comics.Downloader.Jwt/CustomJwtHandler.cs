using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comics.Downloader.Jwt
{
    public class CustomJwtHandler : JwtBearerHandler
    {
        private readonly IOptionsMonitor<JwtBearerOptions> _options;


        public CustomJwtHandler(IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
            _options = options;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var tokenString = Context.Request.Headers.Authorization.FirstOrDefault()?.Substring("Bearer ".Length).Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(tokenString))
            {
                return AuthenticateResult.Fail("No Bearer token in headers");
            }

            //return AuthenticateResult.NoResult(); //Todo chagne this later
            if (!JwtTokenUtility.ValidateToken(tokenString, _options.ToString())) 
            {
                var error = new HttpRequestException("Invalid token", null, HttpStatusCode.Forbidden);
                throw error;
            }

            var principal = GetClaims(tokenString);

            return AuthenticateResult.Success(new AuthenticationTicket(principal, "CustomJwtBearer"));
        }

        private ClaimsPrincipal GetClaims(string Token)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadToken(Token) as JwtSecurityToken;

            var claimsIdentity = new ClaimsIdentity(token.Claims, "Token");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            return claimsPrincipal;
        }
    }
}
