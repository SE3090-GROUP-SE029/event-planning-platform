using System.Text.RegularExpressions;
using Application.Dtos.Plans;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services.Validation;

public sealed partial class CoordinatorPlanValidationService(
    ILogger<CoordinatorPlanValidationService> logger) : ICoordinatorPlanValidationService
{
    private static readonly string[] KnownVendorWords =
        ["hotel", "catering", "photography", "venue", "sodexo", "marriott", "hilton"];

    public CoordinatorPlanValidationResult ValidateCoordinatorPlan(
        CoordinatorPlanResponse response,
        Event eventEntity)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(eventEntity);

        var errors = new List<string>();
        var warnings = new List<string>();
        var budget = ValidateBudgetAllocation(response.BudgetAllocation, eventEntity.Budget);
        errors.AddRange(budget.Errors);
        var categories = ValidateServiceCategories(response.ServiceCategories);
        errors.AddRange(categories.Errors);
        var score = ValidateCompletenessScore(response.PlanCompletenessScore);
        errors.AddRange(score.Errors);
        var risks = ValidateRisks(response.IdentifiedRisks);
        errors.AddRange(risks.Errors);

        if (response.MissingRequirements.Count > 10)
            errors.Add("missingRequirements must contain at most 10 items.");
        foreach (var requirement in response.MissingRequirements)
        {
            if (string.IsNullOrWhiteSpace(requirement.Requirement) ||
                requirement.Requirement.Length > 200)
                errors.Add("Each missing requirement must contain 1-200 characters.");
            if (string.IsNullOrWhiteSpace(requirement.Reason) ||
                requirement.Reason.Length > 500)
                errors.Add("Each missing requirement reason must contain 1-500 characters.");
        }

        if (response.ProposedTimeline.Count < 3)
            errors.Add("proposedTimeline must contain at least 3 phases.");
        if (response.ProposedTimeline.Any(item =>
                string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value)))
            errors.Add("Every timeline phase must have a name and description.");

        if (string.IsNullOrWhiteSpace(response.Rationale) ||
            response.Rationale.Length < 100)
            errors.Add("rationale must contain at least 100 characters.");
        else if (response.Rationale.Length > 2000)
            errors.Add("rationale must contain at most 2000 characters.");
        if (response.ServiceCategories.Any(ContainsVendorName))
            warnings.Add("A service category resembles a vendor or named provider.");
        if (response.ProposedTimeline.Count > 0 &&
            response.ProposedTimeline.Keys.Any(key => key.Contains("conflict", StringComparison.OrdinalIgnoreCase)))
            warnings.Add("Timeline phases should be reviewed for logical ordering.");
        if (response.Rationale?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 15)
            warnings.Add("Rationale may not contain substantive planning detail.");

        var result = new CoordinatorPlanValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            Summary = errors.Count == 0
                ? "Coordinator plan passed deterministic validation."
                : $"Coordinator plan failed validation with {errors.Count} error(s)."
        };
        logger.LogInformation(
            "Coordinator plan validation completed. Valid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}",
            result.IsValid, errors.Count, warnings.Count);
        return result;
    }

    public (bool IsValid, List<string> Errors) ValidateBudgetAllocation(
        Dictionary<string, decimal> allocation, decimal eventBudget)
    {
        var errors = new List<string>();
        if (allocation.Count == 0) errors.Add("budgetAllocation must contain at least one category.");
        if (allocation.Values.Any(value => value < 0)) errors.Add("Budget amounts cannot be negative.");
        var total = allocation.Values.Sum();
        if (total <= 0) errors.Add("Total budget allocation must be greater than zero.");
        if (total > eventBudget)
            errors.Add($"Budget allocation {total:C} exceeds event budget {eventBudget:C}.");
        if (allocation.Keys.Any(string.IsNullOrWhiteSpace))
            errors.Add("Budget allocation category names cannot be empty.");
        return (!errors.Any(), errors);
    }

    public (bool IsValid, List<string> Errors) ValidateServiceCategories(List<string> categories)
    {
        var errors = new List<string>();
        if (categories.Count == 0) errors.Add("At least one service category is required.");
        if (categories.Count > 15) errors.Add("A maximum of 15 service categories is allowed.");
        if (categories.Any(category => string.IsNullOrWhiteSpace(category) || category.Length is < 2 or > 50))
            errors.Add("Each service category must contain 2-50 non-empty characters.");
        if (categories.Count != categories.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            errors.Add("Service categories cannot contain duplicates.");
        return (!errors.Any(), errors);
    }

    public (bool IsValid, List<string> Errors) ValidateCompletenessScore(int score) =>
        score is < 0 or > 100
            ? (false, ["Plan completeness score must be between 0 and 100."])
            : (true, []);

    public (bool IsValid, List<string> Errors) ValidateRisks(List<RiskDto> risks)
    {
        var errors = new List<string>();
        if (risks.Count > 10) errors.Add("A maximum of 10 risks is allowed.");
        foreach (var risk in risks)
        {
            if (string.IsNullOrWhiteSpace(risk.Risk) || risk.Risk.Length > 500)
                errors.Add("Risk descriptions must contain 1-500 characters.");
            if (string.IsNullOrWhiteSpace(risk.Recommendation) || risk.Recommendation.Length > 1000)
                errors.Add("Risk recommendations must contain 1-1000 characters.");
            if (risk.Severity is not ("Low" or "Medium" or "High"))
                errors.Add("Risk severity must be Low, Medium, or High.");
        }
        return (!errors.Any(), errors);
    }

    private bool ContainsVendorName(string value) =>
        KnownVendorWords.Any(word => value.Contains(word, StringComparison.OrdinalIgnoreCase)) ||
        VendorLikePattern().IsMatch(value);

    [GeneratedRegex(@"[&@/]|\b[A-Z][a-z]+\s+(Hotel|Catering|Photography)\b")]
    private static partial Regex VendorLikePattern();
}
