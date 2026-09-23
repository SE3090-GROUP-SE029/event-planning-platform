using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.GuestManagement;

public record QuestionSelection(
    [Required, StringLength(500)] string Question,
    [property: JsonRequired] bool Required);
public record QuestionSuggestions(IReadOnlyList<QuestionSelection> Questions);
public record AnswerSubmission(Guid QuestionId, [Required, StringLength(4000)] string Answer);
public record AiQuestionAnswer(string Question, bool Required, string? Answer);

public interface IRegistrationQuestionClient
{
    Task<QuestionSuggestions> SuggestAsync(AiEventContext context, CancellationToken ct);
}
