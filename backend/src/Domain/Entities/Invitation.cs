using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class Invitation
{
    [Key]
    private int Id { set; get; }
    private int GuestId { set; get; }
    private int EventId { set; get; }
    private string InvitationToken { set; get; }
    private DateAndTime SentAt { set; get; }
    private DateAndTime TokenExpiresAt { set; get; }
    private RsvpStatus RsvpStatus { set; get; }
    private DateAndTime RsvpedAt { set; get; }
    private DateAndTime CreatedAt { set; get; }
    private DateAndTime UpdatedAt { set; get; }
    
}