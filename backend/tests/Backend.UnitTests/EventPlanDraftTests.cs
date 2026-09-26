using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Backend.UnitTests;

public class EventPlanDraftTests
{
    [Fact]
    public void PlanStatusRules_AllowOnlyDocumentedTransitions()
    {
        Assert.True(PlanStatusRules.IsValidTransition(
            PlanStatus.PendingPlannerReview,
            PlanStatus.Approved));
        Assert.True(PlanStatusRules.IsValidTransition(
            PlanStatus.PendingPlannerReview,
            PlanStatus.Rejected));
        Assert.True(PlanStatusRules.IsValidTransition(
            PlanStatus.Approved,
            PlanStatus.Superseded));
        Assert.False(PlanStatusRules.IsValidTransition(
            PlanStatus.Approved,
            PlanStatus.Approved));
    }

    [Fact]
    public void ValueObjects_RejectInvalidTextAndSeverity()
    {
        Assert.Throws<ArgumentException>(() =>
            new IdentifiedRisk("", RiskSeverity.Low, "Add a backup supplier."));
        Assert.Throws<ArgumentException>(() =>
            new MissingRequirement("Catering", "", RequirementCategory.Catering));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new IdentifiedRisk("Late delivery", (RiskSeverity)99, "Confirm delivery window."));
    }

    [Fact]
    public void EventPlanDraftConfiguration_AddsVersionIndexAndJsonColumns()
    {
        using var db = CreateDb();
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(EventPlanDraft));

        Assert.NotNull(entityType);
        Assert.Contains(
            entityType!.GetIndexes(),
            index => index.IsUnique &&
                index.Properties.Select(property => property.Name)
                    .SequenceEqual([nameof(EventPlanDraft.EventId), nameof(EventPlanDraft.Version)]));
        Assert.Equal(
            "jsonb",
            entityType.FindProperty(nameof(EventPlanDraft.EventSnapshot))!
                .FindAnnotation("Relational:ColumnType")!.Value);
        Assert.Equal(
            "jsonb",
            entityType.FindProperty(nameof(EventPlanDraft.BudgetAllocation))!
                .FindAnnotation("Relational:ColumnType")!.Value);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
