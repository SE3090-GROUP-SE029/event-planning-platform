namespace Domain.Entities;

public class User
{
    public Guid Id {get; set;}
    public string Email {get; set;} = default!;
    public string PasswordHash {get; set;} = default!;
    public string FirstName {set; get;} = default!;
    public string LastName {set; get;} = default!;
    public DateTime CreatedAt {set; get;}
    public DateTime? UpdatedAt {get; set;}
    public bool IsActive {get; set;} = true;
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<EventPlanDraft> CreatedEventPlanDrafts { get; set; } = [];
    public ICollection<EventPlanDraft> ApprovedEventPlanDrafts { get; set; } = [];
    public ICollection<EventPlanDraft> RejectedEventPlanDrafts { get; set; } = [];
}