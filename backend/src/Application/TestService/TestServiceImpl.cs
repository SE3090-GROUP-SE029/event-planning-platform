using Application.TestSerivce;
using Domain.Entities;

namespace Application.Services;

public class TestSerivce : ITestService
{
    private static int _messageId = 1;
    private static readonly Dictionary<int, TestMessage> _messages = new();

    public Task<TestMessage> CreateTestMessageAsync(string message)
    {
        var msg = new TestMessage
        {
            Id = _messageId++,
            Message = message,
            CreatedAt = DateTime.Now
        };
        return Task.FromResult(msg);
    }

    public Task<TestMessage> GetTestMessageAsync(int id)
    {
        if (_messages.TryGetValue(id, out var message))
            return Task.FromResult(message);
        throw new KeyNotFoundException($"Message {id} not found");
    }

}