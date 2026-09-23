using Domain.Enums;

namespace Domain.Entities;

public class RegistrationForm
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public DateTimeOffset OpensAt { get; set; }
    public DateTimeOffset ClosesAt { get; set; }
    public int SeatLimit { get; set; }
    public RegistrationFormStatus Status { get; set; }
    public string? PublicId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<RegistrationQuestion> Questions { get; set; } = [];
}
