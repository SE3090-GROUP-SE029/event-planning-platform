using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class VenderService
{
    [Key]
    private int Id { set; get; }
    private string ServiceName { set; get; }
    private int vendorId { set; get; }
    private string ServiceDescription { set; get; }
    private string Category { set; get; }
    private DateAndTime CreatedAt { set; get; }
    private DateAndTime UpdatedAt { set; get; }
    private bool IsActive { set; get; }
}