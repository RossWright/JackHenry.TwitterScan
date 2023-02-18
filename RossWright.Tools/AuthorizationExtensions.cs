using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RossWright;

public static class AuthorizationExtensions
{
    public static AuthConfig AddJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var authConfig = new AuthConfig();
        config.Bind(nameof(AuthConfig), authConfig);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = authConfig.JwtIssuer,
                    ValidAudience = authConfig.JwtAudience,
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(authConfig.IssuerSigningKey)),
                    RequireExpirationTime = false
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            context.Response.Headers.Add("Token-Expired", "true");
                        return Task.CompletedTask;
                    }
                };
            });

        return authConfig;
    }
}
public class AuthConfig
{
    public string JwtIssuer { get; set; } = null!;
    public string JwtAudience { get; set; } = null!;
    public string IssuerSigningKey { get; set; } = null!;
    public string MakeAccessToken(Claim[]? claims = null, DateTime? expires = null) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(JwtIssuer, JwtAudience,
            claims: claims,
            expires: expires ?? DateTime.MaxValue,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(IssuerSigningKey)), SecurityAlgorithms.HmacSha256)));
}
