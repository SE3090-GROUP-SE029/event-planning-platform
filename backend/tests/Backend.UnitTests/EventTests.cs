using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Backend.UnitTests;

public class EventTests
{
    [Fact]
    public void NewEvent_DefaultsToDraft()
    {
        var eventEntity = new Event();

        Assert.Equal(EventStatus.DRAFT, eventEntity.Status);
    }

    [Fact]
    public void EventStatus_ContainsExpectedLifecycleValues()
    {
        Assert.Equal(
            [EventStatus.DRAFT, EventStatus.PLANNING, EventStatus.CONFIRMED, EventStatus.COMPLETED, EventStatus.CANCELLED],
            Enum.GetValues<EventStatus>());
    }

    [Fact]
    public void EventConfiguration_MapsRequiredPropertiesAndDefaults()
    {
        using var db = CreateDb();
        var entityType = db.Model.FindEntityType(typeof(Event));

        Assert.NotNull(entityType);
        Assert.Equal("Events", entityType!.GetTableName());
        Assert.False(entityType.FindProperty(nameof(Event.EventType))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(Event.PreferredDate))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(Event.Status))!.IsNullable);
        Assert.Equal(EventStatus.DRAFT, entityType.FindProperty(nameof(Event.Status))!.GetDefaultValue());
    }

    [Fact]
    public void EventConfiguration_AddsGuestCountAndBudgetCheckConstraints()
    {
        using var db = CreateDb();
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Event));
        var constraints = entityType!.GetCheckConstraints();

        Assert.Equal(
            "\"GuestCount\" > 0",
            constraints.Single(c => c.Name == "CK_Events_GuestCount_Positive").Sql);
        Assert.Equal(
            "\"Budget\" >= 0",
            constraints.Single(c => c.Name == "CK_Events_Budget_NonNegative").Sql);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
