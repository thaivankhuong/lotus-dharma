namespace LotusDharma.Application.Identity.DTOs;

public record AuthResponseDto
(
    string Token,
    string RefreshToken,
    DateTimeOffset Expiration,
    int UserId,
    string Email,
    string UserName,
    List<string> Roles
);


