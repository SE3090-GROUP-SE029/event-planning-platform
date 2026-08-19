using Api.Dtos.Requests;
using Api.Dtos.Responses;
using Application.TestSerivce;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ITestService _testService;

    public TestController(ITestService testService)
    {
        _testService = testService;
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong", timestamp = DateTime.Now});
    }

    [HttpPost("message")]
    public async Task<IActionResult> CreateMessage([FromBody] CreateTestMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message)) 
            return BadRequest(new { error = "Message cannot be empty"});
        
        var msg = await _testService.CreateTestMessageAsync(request.Message);
        var response = new TestMessageResponse
        {
            Id = msg.Id,
            Message = msg.Message,
            CreatedAt = msg.CreatedAt
        };

        return CreatedAtAction(nameof(GetMessage), new { id = msg.Id }, response);
    } 

    public async Task<IActionResult> GetMessage(int id)
    {
        try
        {
            var msg = await _testService.GetTestMessageAsync(id);
            var response = new TestMessageResponse
            {
                Id = msg.Id,
                Message = msg.Message,
                CreatedAt = msg.CreatedAt
            };
            return Ok(response);
        }
        catch
        {
            return NotFound(new{ error = $"Message {id} not found" });
        }
    }
}