using Domain.Entities;

namespace Application.Services.Test;

public class TestService : ITestService
{
    private static int _messageId = 1;
    private static readonly Dictionary<int, TestMessage> _messages = new();
    private static readonly object _lock = new();

    public Task<TestMessage> CreateTestMessageAsync(string message)
    {
        lock (_lock)
        {
            var msg = new TestMessage
            {
                Id = _messageId++,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };
            _messages[msg.Id] = msg;
            return Task.FromResult(msg);
        }
    }

    public Task<TestMessage> GetTestMessageAsync(int id)
    {
        lock (_lock)
        {
            if (_messages.TryGetValue(id, out var message))
                return Task.FromResult(message);
            throw new KeyNotFoundException($"Message {id} not found");
        }
    }
}
