namespace Domain.Entities;

public class RegistrationQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RegistrationFormId { get; set; }
    public RegistrationForm RegistrationForm { get; set; } = null!;
    public string Question { get; set; } = string.Empty;
    public bool Required { get; set; }
    public int DisplayOrder { get; set; }
    // Retain previous draft selections without deleting records.
    public bool IsSelected { get; set; } = true;
}
