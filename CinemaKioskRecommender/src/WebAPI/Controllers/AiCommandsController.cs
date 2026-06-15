using CinemaKioskRecommender.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace CinemaKioskRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/ai")]
public class AiCommandsController : ControllerBase
{
    private static readonly ConcurrentDictionary<Guid, ConcurrentQueue<AiCommandDto>> _commands = new();

    [HttpPost("{sessionId:guid}/command")]
    public IActionResult SendCommand(Guid sessionId, [FromBody] AiCommandDto request)
    {
        var queue = _commands.GetOrAdd(sessionId, _ => new ConcurrentQueue<AiCommandDto>());
        queue.Enqueue(request);
        return Ok(new { success = true });
    }

    [HttpGet("{sessionId:guid}/commands")]
    public IActionResult GetCommands(Guid sessionId)
    {
        if (!_commands.TryGetValue(sessionId, out var queue) || queue.IsEmpty)
            return Ok(new List<AiCommandDto>());

        var commands = queue.ToList();
        queue.Clear();
        return Ok(commands);
    }
}
