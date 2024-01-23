using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;

namespace Comics.Downloader.Jwt
{
    public static class JwtTokenUtility
    {
        public static string GenerateToken(string secret, List<KeyValuePair<string, string>> subjects)
        {
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(secret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(subjects.Select(x => new Claim(x.Key, x.Value))),
                    Expires = DateTime.UtcNow.AddHours(4),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
        }

        public static Dictionary<string, string> ToDictionary(this string token)
        {
            var result = new Dictionary<string, string>();
            var h = new JwtSecurityTokenHandler();
            var tokenDetail = h.ReadJwtToken(token);
            return tokenDetail.Claims.ToList().ToDictionary(x => x.Type, x => x.Value);
        }


        public static bool ValidateToken(string token, string secret)
        {
            if (token.IsNullOrEmpty())
            {
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                tokenHandler.ValidateToken(token, validationParameters: GetTokenValidationParameters(secret), out SecurityToken validatedToken);

                return validatedToken is not null;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static TokenValidationParameters GetTokenValidationParameters(string secret)
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero,
            };
        }
    }
}
