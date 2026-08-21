using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class AiValidationResult
{
    [Key]
    private int Id { set; get; }
    private int AiValidationRequestId { set; get; }
    private int EventId { set; get; }
    private DateAndTime ValidatedAt { set; get; }
    private ValidationStatus ValidationStatus { set; get; }
    private string ConstraintsJson { set; get; }
    private string OverallSummary { set; get; }
    private string FailedReasons { set; get; }
    private bool AllowUserOverride { set; get; }
    private SourceAgent SourceAgent { set; get; }
}