using Domain.Entities;

namespace Application.Services.Test;

public interface ITestService
{
    Task<TestMessage> CreateTestMessageAsync(string message);
    Task<TestMessage> GetTestMessageAsync(int id);
}
