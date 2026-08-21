using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class RefreshToken
{
    [Key]
    private int Id { set; get; }
    private int UserId { set; get; }
    private string Token { set; get; }
    private DateAndTime ExpiresAt { set; get; }
    private DateAndTime RevokedAt { set; get; }
    private DateAndTime CreatedAt { set; get; }
    private string IpAdress { set; get; }
}