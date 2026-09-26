using Application.Dtos.Plans;
using Domain.Entities;

namespace Application.Services.Validation;

public interface ICoordinatorPlanValidationService
{
    CoordinatorPlanValidationResult ValidateCoordinatorPlan(
        CoordinatorPlanResponse response,
        Event eventEntity);

    (bool IsValid, List<string> Errors) ValidateBudgetAllocation(
        Dictionary<string, decimal> allocation,
        decimal eventBudget);

    (bool IsValid, List<string> Errors) ValidateServiceCategories(List<string> categories);
    (bool IsValid, List<string> Errors) ValidateCompletenessScore(int score);
    (bool IsValid, List<string> Errors) ValidateRisks(List<RiskDto> risks);
}
