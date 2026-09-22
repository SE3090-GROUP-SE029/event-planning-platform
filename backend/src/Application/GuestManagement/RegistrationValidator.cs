using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace Application.GuestManagement;

public static class RegistrationValidator
{
    public static IReadOnlyList<QuestionSelection> ValidateQuestions(IReadOnlyList<QuestionSelection>? questions)
    {
        if (questions is null || questions.Count > 10 || questions.Any(q => q is null))
            throw new RegistrationException(400, "invalid_questions", "Provide at most 10 questions.");
        var result = questions.Select(q => q with { Question = Required(q.Question, 500, "Question") }).ToArray();
        if (result.Select(q => q.Question).Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.Length)
            throw new RegistrationException(400, "invalid_questions", "Questions must be distinct.");
        return result;
    }

    public static IReadOnlyList<AnswerSubmission> ValidateAnswers(Domain.Entities.RegistrationForm form,
        IReadOnlyList<AnswerSubmission>? answers)
    {
        answers ??= [];
        var questions = form.Questions.Where(q => q.IsSelected).ToDictionary(q => q.Id);
        if (answers.Count > 10 || answers.Any(a => a is null) || answers.Select(a => a.QuestionId).Distinct().Count() != answers.Count ||
            answers.Any(a => !questions.ContainsKey(a.QuestionId)))
            throw new RegistrationException(400, "invalid_answers", "Answers must reference distinct questions on this published form.");
        var result = answers.Select(a => a with { Answer = Required(a.Answer, 4000, "Answer") }).ToArray();
        if (questions.Values.Any(q => q.Required && !result.Any(a => a.QuestionId == q.Id)))
            throw new RegistrationException(400, "required_answer", "Answer all required registration questions.");
        return result;
    }

    public static FormSettings Validate(FormSettings settings)
    {
        if (settings.OpensAt == default || settings.ClosesAt == default || settings.OpensAt >= settings.ClosesAt)
            throw new RegistrationException(400, "invalid_period", "Opening time must be before closing time.");
        ValidateSeatLimit(settings.SeatLimit);
        return settings with { OpensAt = settings.OpensAt.ToUniversalTime(), ClosesAt = settings.ClosesAt.ToUniversalTime() };
    }

    public static void ValidateSeatLimit(int seatLimit)
    {
        if (seatLimit <= 0)
            throw new RegistrationException(400, "invalid_capacity", "Seat limit must be positive.");
    }

    public static GuestDetails Validate(GuestDetails details)
    {
        var name = Required(details.FullName, 200, "Full name");
        var email = Required(details.EmailAddress, 254, "Email address");
        if (!new EmailAddressAttribute().IsValid(email) || !MailAddress.TryCreate(email, out var parsed) ||
            !string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase))
            throw new RegistrationException(400, "invalid_email", "A valid email address is required.");
        return new GuestDetails(name, email, Optional(details.Organisation, 200, "Organisation"),
            Optional(details.PhoneNumber, 40, "Phone number"));
    }

    public static void ValidatePublicCredential(string value)
    {
        if (value.Length != 43 || value.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_'))
            throw new RegistrationException(404, "not_found", "Registration resource not found.");
    }

    private static string Required(string? value, int maximum, string name)
        => Optional(value, maximum, name) ?? throw new RegistrationException(400, "required_field", $"{name} is required.");

    private static string? Optional(string? value, int maximum, string name)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (value.Length > maximum || value.Any(char.IsControl))
            throw new RegistrationException(400, "invalid_field", $"{name} is too long or contains control characters.");
        return value;
    }
}
