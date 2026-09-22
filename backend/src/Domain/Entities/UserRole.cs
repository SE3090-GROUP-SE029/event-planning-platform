namespace Domain.Entities;

public class UserRole
{
    public Guid Id {get; set;} 
    public Guid UserId { get; set; }
    public User User {get; set;} = default!;
    public Guid RoleId { get; set; }
    public Role Role {get; set;} = default!;
    public DateTime AssignedAt {get; set;}
    public Guid? AssignedByUserId {get; set;}
}