using Application.Dtos.Events;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public sealed class EventRepositoryTests
{
    [Fact]
    public async Task ListAdminAsync_searches_event_name_case_insensitively_and_applies_inclusive_date_range()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        db.Events.AddRange(
            CreateEvent("Autumn Celebration", new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc)),
            CreateEvent("Autumn Afterparty", new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc)),
            CreateEvent("Winter Gala", new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var repository = new EventRepository(db);
        var (items, total) = await repository.ListAdminAsync(new AdminEventQuery
        {
            Search = "CELEBRATION",
            DateFrom = new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc),
            DateTo = new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc),
            SortBy = "preferredDate",
            SortOrder = "asc"
        });

        Assert.Equal(1, total);
        Assert.Equal("Autumn Celebration", Assert.Single(items).EventName);
    }

    [Fact]
    public async Task ListAsync_searches_only_the_requested_owners_events()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var ownerId = Guid.NewGuid();
        db.Events.AddRange(
            CreateEvent("Autumn Celebration", new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc), ownerId),
            CreateEvent("Autumn Celebration", new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var repository = new EventRepository(db);
        var (items, total) = await repository.ListAsync(ownerId, new EventQuery
        {
            Search = "celebration",
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(1, total);
        Assert.Equal(ownerId, Assert.Single(items).OwnerId);
    }

    private static Event CreateEvent(string eventName, DateTime preferredDate, Guid? ownerId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId ?? Guid.NewGuid(),
            EventName = eventName,
            EventType = EventType.CORPORATE,
            GuestCount = 10,
            Budget = 1000,
            PreferredVenue = "Main Hall",
            PreferredDate = preferredDate,
            EventDuration = TimeSpan.FromHours(2),
            Status = EventStatus.DRAFT,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
}
