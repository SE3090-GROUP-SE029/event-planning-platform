using Application.Dtos.Plans;
using Application.Services.Validation;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.UnitTests;

public sealed class CoordinatorPlanValidationServiceTests
{
    private readonly CoordinatorPlanValidationService _service =
        new(NullLogger<CoordinatorPlanValidationService>.Instance);

    [Fact]
    public void ValidateBudgetAllocation_RejectsOverflowAndNegativeValues()
    {
        var result = _service.ValidateBudgetAllocation(
            new Dictionary<string, decimal>
            {
                ["Venue"] = 1200m,
                ["Catering"] = -1m
            },
            1000m);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("exceeds event budget"));
        Assert.Contains(result.Errors, error => error.Contains("negative"));
    }

    [Fact]
    public void ValidateCoordinatorPlan_SeparatesVendorWarningFromCriticalErrors()
    {
        var response = new CoordinatorPlanResponse
        {
            ServiceCategories = ["Four Seasons Hotel"],
            BudgetAllocation = new Dictionary<string, decimal> { ["Four Seasons Hotel"] = 100m },
            TargetVendorTypes = ["Venue provider"],
            ProposedTimeline = new Dictionary<string, string>
            {
                ["Planning"] = "12 weeks before",
                ["Booking"] = "8 weeks before",
                ["Confirmation"] = "1 week before"
            },
            Rationale = new string('A', 120),
            PlanCompletenessScore = 80
        };
        var eventEntity = new Event { Budget = 1000m };

        var result = _service.ValidateCoordinatorPlan(response, eventEntity);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, warning => warning.Contains("vendor"));
    }

    [Fact]
    public void ValidateRisks_RejectsInvalidSeverityAndLengths()
    {
        var result = _service.ValidateRisks(
        [
            new RiskDto
            {
                Risk = string.Empty,
                Severity = "Critical",
                Recommendation = new string('x', 1001)
            }
        ]);

        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }
}
