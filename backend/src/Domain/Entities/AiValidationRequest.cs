using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class AiValidationRequest
{
    [Key]
    private int Id { set; get; }
    private int EventId { set; get; }
    private ProposalType ProposalType { set; get; }
    private SourceAgent SourceAgent { set; get; }
    private string ProposalDataJson { set; get; }
    private DateAndTime SubmittedAt { set; get; }
    private DateAndTime CreatedAt { set; get; }
    private DateAndTime UpdatedAt { set; get; }
}