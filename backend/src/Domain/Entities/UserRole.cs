namespace Domain.Entities;

public class UserRole
{
    public Guid Id {get; set;} 
    public User User {get; set;} = default!;
    public Role Role {get; set;} = default!;
    public DateTime AssignedAt {get; set;}
    public Guid? AssignedByUserId {get; set;}
}