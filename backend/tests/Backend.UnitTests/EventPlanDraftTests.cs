using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
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

    [Fact]
    public void EventSnapshotJsonConverter_RoundTripsCurrentPayload()
    {
        using var db = CreateDb();
        var property = db.Model.FindEntityType(typeof(EventPlanDraft))!
            .FindProperty(nameof(EventPlanDraft.EventSnapshot))!;
        var converter = property.GetValueConverter()!;
        var snapshot = CreateSnapshot();
        var previousPayload = JsonSerializer.Serialize(
            snapshot,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var json = Assert.IsType<string>(converter.ConvertToProvider(snapshot));
        var restored = Assert.IsType<EventSnapshot>(converter.ConvertFromProvider(json));

        Assert.Equal(previousPayload, json);
        Assert.Equal(snapshot.EventName, restored.EventName);
        Assert.Equal(snapshot.EventType, restored.EventType);
        Assert.Equal(snapshot.EventDate, restored.EventDate);
        Assert.Equal(snapshot.Location, restored.Location);
        Assert.Equal(snapshot.GuestCount, restored.GuestCount);
        Assert.Equal(snapshot.Budget, restored.Budget);
        Assert.Equal(snapshot.Requirements, restored.Requirements);
        Assert.Equal(snapshot.CreatedAt, restored.CreatedAt);
        Assert.Equal(snapshot.LastModifiedAt, restored.LastModifiedAt);
        Assert.Contains("\"eventName\"", json);
    }

    [Fact]
    public void EventSnapshotJsonConverter_ReadsLegacyPascalCasePayloadAndPastEventDate()
    {
        using var db = CreateDb();
        var converter = db.Model.FindEntityType(typeof(EventPlanDraft))!
            .FindProperty(nameof(EventPlanDraft.EventSnapshot))!
            .GetValueConverter()!;
        const string legacyJson =
            """{"EventName":"Legacy celebration","EventType":0,"EventDate":"2020-01-01T12:00:00","Location":"Old Hall","GuestCount":80,"Budget":2500.00,"Requirements":["Accessible entrance"],"CreatedAt":"2019-12-01T10:00:00"}""";

        var restored = Assert.IsType<EventSnapshot>(converter.ConvertFromProvider(legacyJson));

        Assert.Equal("Legacy celebration", restored.EventName);
        Assert.Equal(new DateTime(2020, 1, 1, 12, 0, 0), restored.EventDate);
        Assert.Equal("Old Hall", restored.Location);
        Assert.Equal(["Accessible entrance"], restored.Requirements);
        Assert.Equal(default, restored.LastModifiedAt);
    }

    [Fact]
    public void EventSnapshotJsonConverter_ThrowsWithRawJsonForCorruptedPayload()
    {
        using var db = CreateDb();
        var converter = db.Model.FindEntityType(typeof(EventPlanDraft))!
            .FindProperty(nameof(EventPlanDraft.EventSnapshot))!
            .GetValueConverter()!;
        const string corruptedJson = """{"EventName":""";

        var exception = Assert.Throws<Application.Common.Exceptions.PersistedJsonDeserializationException>(
            () => converter.ConvertFromProvider(corruptedJson));

        Assert.Equal(corruptedJson, exception.RawJson);
        Assert.Equal(nameof(EventSnapshot), exception.DataType);
    }

    [Fact]
    public async Task GeneratedPlanSnapshot_CanBeSavedAndRetrieved()
    {
        using var db = CreateDb();
        var eventEntity = new Event { Id = Guid.NewGuid(), EventName = "Saved event" };
        var plan = new EventPlanDraft
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            Event = eventEntity,
            Version = 1,
            ServiceCategories = ["Catering"],
            EventSnapshot = CreateSnapshot()
        };
        db.Events.Add(eventEntity);
        db.EventPlanDrafts.Add(plan);

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var restored = await new EventPlanDraftRepository(db).GetByIdAsync(plan.Id);

        Assert.NotNull(restored);
        Assert.Equal("Snapshot event", restored!.EventSnapshot.EventName);
        Assert.Equal(["Venue", "Catering"], restored.EventSnapshot.Requirements);
        Assert.Equal(eventEntity.Id, restored.EventId);
    }

    private static EventSnapshot CreateSnapshot() =>
        new(
            "Snapshot event",
            EventType.WEDDING,
            DateTime.UtcNow.AddDays(30),
            "Community Hall",
            50,
            5000m,
            ["Venue", "Catering"],
            DateTime.UtcNow,
            DateTime.UtcNow);

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
