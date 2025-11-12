using Google.Apis.Auth;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Models;
using Microsoft.Extensions.Configuration;

namespace LotusDharma.Infrastructure.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            return null;

        try
        {
            var clientId = _configuration["GoogleAuth:ClientId"];
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

            return new GoogleUserInfo(
                Sub: payload.Subject,
                Email: payload.Email,
                Name: payload.Name,
                Picture: payload.Picture
            );
        }
        catch
        {
            // Validation failed
            return null;
        }
    }
}


