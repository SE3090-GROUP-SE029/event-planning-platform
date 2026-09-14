namespace Domain.Entities;

public class TestMessage
{
    public int Id { get; set; }
    public string Message { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}