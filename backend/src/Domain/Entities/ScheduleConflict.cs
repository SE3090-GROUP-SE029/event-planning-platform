using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class ScheduleConflict
{
    [Key]
    private int Id { set; get; }
    private int ScheduleId { set; get; }
    private int ActivityOneId { set; get; }
    private int ActivityTwoId { set; get; }
    private ConflictType ConflictType { set; get; }
    private string ConflictDescription { set; get; }
    private DateAndTime ResolvedAt { set; get; }
    private string ResolutionMethod { set; get; }
    private DateAndTime CreatedAt { set; get; }

}