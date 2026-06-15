namespace CinemaKioskRecommender.Application.DTOs;

public class AiCommandDto
{
    public string Command { get; set; } = string.Empty;
    public Dictionary<string, object> Payload { get; set; } = new();
}
