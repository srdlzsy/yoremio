using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Extensions
{
    public static class JwtTokenGenerator
    {
        public static string JwtGenerateToken(this ApplicationUser user, IConfiguration configuration, string role)
        {
            return user.JwtGenerateToken(configuration, new[] { role });
        }

        public static string JwtGenerateToken(this ApplicationUser user, IConfiguration configuration, IEnumerable<string> roles)
        {
            var roleList = roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (roleList.Length == 0)
            {
                throw new Exception("JWT icin en az bir rol gereklidir.");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new(ClaimTypes.Name, user.UserName ?? ""),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roleList.Select(role => new Claim(ClaimTypes.Role, role)));

            var keyString = configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(keyString))
            {
                throw new Exception("JWT Key yapılandırması eksik.");
            }

            var expireMinutes = configuration.GetValue<int?>("Jwt:ExpireMinutes") ?? 60;
            if (expireMinutes <= 0)
            {
                expireMinutes = 60;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
