namespace LotusDharma.Application.Common.Interfaces;

public interface IGoogleAuthService
{
    /// <summary>
    /// Validate the provided Google ID token and return basic user info if valid.
    /// Returns null when validation fails.
    /// </summary>
    Task<LotusDharma.Application.Common.Models.GoogleUserInfo?> ValidateIdTokenAsync(string idToken);
}


