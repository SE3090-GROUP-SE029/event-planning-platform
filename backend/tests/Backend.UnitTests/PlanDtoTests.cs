using System.Text.Json;
using Application.Dtos.Common;
using Application.Dtos.Plans;

namespace Backend.UnitTests;

public class PlanDtoTests
{
    [Fact]
    public void CoordinatorPlanResponse_ValidatesBudgetAndNestedDtos()
    {
        var response = new CoordinatorPlanResponse
        {
            ServiceCategories = ["Venue"],
            BudgetAllocation = new() { ["Venue"] = 150_000m },
            PlanCompletenessScore = 82,
            IdentifiedRisks =
            [
                new RiskDto
                {
                    Risk = "Capacity constraint",
                    Severity = "High",
                    Recommendation = "Confirm capacity."
                }
            ]
        };

        Assert.True(response.IsValid(200_000m));
        Assert.False(response.IsValid(100_000m));
        Assert.Contains(response.GetValidationErrors(100_000m), error => error.MemberNames.Contains("BudgetAllocation"));
    }

    [Fact]
    public void Requests_ValidateConditionalFields()
    {
        var generate = new GeneratePlanRequest { Regenerate = true };
        var reject = new RejectPlanRequest { Remarks = "Too short" };

        Assert.Contains(generate.GetValidationErrors(), error => error.MemberNames.Contains("EventId"));
        Assert.Contains(generate.GetValidationErrors(), error => error.MemberNames.Contains("RegenerationReason"));
        Assert.Contains(reject.GetValidationErrors(), error => error.MemberNames.Contains("PlanId"));
        Assert.Contains(reject.GetValidationErrors(), error => error.MemberNames.Contains("Remarks"));
    }

    [Fact]
    public void PlanDtos_UseCamelCaseJsonProperties()
    {
        var response = new CoordinatorPlanResponse
        {
            ServiceCategories = ["Venue"],
            PlanCompletenessScore = 82
        };

        var json = JsonSerializer.Serialize(response);

        Assert.Contains("\"serviceCategories\"", json);
        Assert.Contains("\"planCompletenessScore\"", json);
        Assert.DoesNotContain("\"ServiceCategories\"", json);
    }

    [Fact]
    public void ApiResponses_ExposeFactoryStatusAndTraceId()
    {
        var response = ApiResponse<string>.NotFound(traceId: "trace-123");
        var error = ApiErrorResponse.Unauthorized(traceId: "trace-456");

        Assert.False(response.Success);
        Assert.Equal(404, response.StatusCode);
        Assert.Equal("trace-123", response.TraceId);
        Assert.Equal(401, error.StatusCode);
        Assert.Equal("trace-456", error.TraceId);
    }
}
