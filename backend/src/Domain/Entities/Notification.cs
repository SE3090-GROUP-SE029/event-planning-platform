using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

public class Notification
{
    [Key]
    private int Id { set; get; }
    private int UserId { set; get; }
    private NotificationType NotificationType { set; get; }
    private string NotificationTitle { set; get; }
    private string NotificationBody { set; get; }
    private DateAndTime ReadAt { set; get; }
    private DateAndTime SentAt { set; get; }
    private string PushTokens { set; get; }

}