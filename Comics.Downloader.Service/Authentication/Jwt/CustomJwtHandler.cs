using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Amazon.Runtime.Internal;
using Comics.Downloader.Model;
using Comics.Downloader.Service.Database;
using Comics.Downloader.Service.Utiliyes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comics.Downloader.Service.Authentication.Jwt
{
    public class CustomJwtHandler : JwtBearerHandler
    {
        private readonly MongoDbContext? _mongoContext;
        private readonly Appsetting _appsetting;

        public CustomJwtHandler(IOptions<Appsetting> appsetting, MongoDbContext? _mongoContext, IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
            _mongoContext = this._mongoContext;
            _appsetting = appsetting.Value;
        }

        public CustomJwtHandler(IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var tokenString = Context.Request.Headers.Authorization.FirstOrDefault()?.Substring("Bearer ".Length).Trim() ?? string.Empty;
            if (tokenString.IsNullOrEmpty())
            {
                return AuthenticateResult.Fail("No Bearer token in headers");
            }

            //return AuthenticateResult.NoResult();
            if (!JwtTokenUtility.ValidateToken(tokenString, _appsetting.Jwt.Secret)) 
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
