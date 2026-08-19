using Domain.Entities;

namespace Application.TestSerivce;

public interface ITestService
{
    Task<TestMessage> CreateTestMessageAsync(string message);
    Task<TestMessage> GetTestMessageAsync(int id);
}

