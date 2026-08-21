using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Role
{
    [Key]
    private int RoleIId { set; get; }
    private string RoleName { set; get; }
}