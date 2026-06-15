using Livekit.Server.Sdk.Dotnet;
using Microsoft.AspNetCore.Mvc;

namespace CinemaKioskRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/livekit")]
public class LiveKitController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public LiveKitController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("token/{sessionId}")]
    public IActionResult GenerateToken(string sessionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { error = "SessionId is required" });

            var apiKey = _configuration["LiveKit:ApiKey"];
            var apiSecret = _configuration["LiveKit:ApiSecret"];
            var livekitUrl = _configuration["LiveKit:Url"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret) || string.IsNullOrEmpty(livekitUrl))
                return BadRequest(new { error = "LiveKit credentials not configured in appsettings.json" });

            var token = new AccessToken(apiKey!, apiSecret!)
                .WithIdentity($"kiosk-user-{sessionId}")
                .WithName("КіноБот Користувач")
                .WithGrants(new VideoGrants
                {
                    RoomJoin = true,
                    Room = sessionId,
                    CanPublish = true,
                    CanSubscribe = true
                })
                .ToJwt();

            return Ok(new { url = livekitUrl, token });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Token generation failed",
                message = ex.Message
            });
        }
    }
}
