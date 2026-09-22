namespace Application.Dtos.Test;

public class TestMessageResponse
{
    public int Id { get; set; }
    public string Message { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
