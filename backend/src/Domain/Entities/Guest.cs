using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class Guest
{
    [Key]
    private int Id { set; get; }
    private int EventId { set; get; }
    private string GuestEmail { set; get; }
    private string GuestName { set; get; }
    private string GuestPhone { set; get; }
    private int PlusOnes { set; get; }
    private DateAndTime CreatedAt { set; get; }
    private DateAndTime UpdatedAt { set; get; }
}