using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class UserRole
{
    [Key]
    private int Id { set; get; }
    private int UserId { set; get; }
    private int RoleId { set; get; }
    private DateAndTime CreatedAt { set; get; }
    private int AssignedByUserId { set; get; }
}