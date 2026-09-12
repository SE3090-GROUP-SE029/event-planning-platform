using Domain.Enums;

namespace Domain.Entities;

public class Role
{
    public Guid Id {get; set;}
    public RoleName RoleName {set; get;} = default!;
    public ICollection<UserRole> UserRoles {get; set;} = [];
}