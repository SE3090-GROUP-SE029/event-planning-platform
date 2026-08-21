using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class CheckIn
{
    [Key]
    private int Id { set; get; }
    private int GuestId { set; get; }
    private int EventId { set; get; }
    private DateAndTime CheckedInAt { set; get; }
    private int CheckedInByUserId { set; get; }
    private  CheckedInMethod CheckedInMethod { set; get; } 
    private string Notes { set; get; }
    private DateAndTime CreatedAt { set; get; }
}