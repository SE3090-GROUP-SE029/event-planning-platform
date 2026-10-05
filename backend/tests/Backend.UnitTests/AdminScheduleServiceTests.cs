using System.Reflection;
using Api.Controllers;
using Application.Dtos.Admin;
using Application.Services.Admin;
using Application.Services.Scheduling;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class AdminScheduleServiceTests
{
    [Fact]
    public async Task ListAsync_returns_schedule_monitoring_fields()
    {
        using var db = CreateDb();
        var planner = SeedPlanner(db);
        var eventEntity = SeedEvent(db, planner.Id, "Annual Gala");
        var schedule = SeedSchedule(db, eventEntity.Id);
        SeedActivity(db, schedule.Id, "Doors open", EventDateAt(18, 0), EventDateAt(18, 30), null);
        SeedActivity(db, schedule.Id, "Dinner", EventDateAt(18, 15), EventDateAt(19, 0), null);
        db.ScheduleConflicts.Add(new ScheduleConflict
        {
            ScheduleId = schedule.Id,
            ActivityId1 = Guid.NewGuid(),
            ActivityId2 = Guid.NewGuid(),
            ConflictType = ConflictDetectionService.ActivityOverlap,
            Description = "Activities overlap.",
            IsResolved = false,
            DetectedAt = DateTime.UtcNow
        });
        db.ScheduleConflicts.Add(new ScheduleConflict
        {
            ScheduleId = schedule.Id,
            ActivityId1 = Guid.NewGuid(),
            ActivityId2 = Guid.NewGuid(),
            ConflictType = ConflictDetectionService.ActivityOverlap,
            Description = "Resolved overlap.",
            IsResolved = true,
            DetectedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).ListAsync(
            new AdminScheduleQuery { Search = "gala", Page = 1, PageSize = 10 },
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(schedule.Id, item.ScheduleId);
        Assert.Equal(eventEntity.Id, item.EventId);
        Assert.Equal("Annual Gala", item.EventTitle);
        Assert.Equal(2, item.ActivityCount);
        Assert.Equal(1, item.UnresolvedConflictCount);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetByIdAsync_returns_chronological_activities_vendors_and_conflicts()
    {
        using var db = CreateDb();
        var planner = SeedPlanner(db);
        var eventEntity = SeedEvent(db, planner.Id, "Launch Night");
        var schedule = SeedSchedule(db, eventEntity.Id);
        var vendor = SeedVendor(db);
        var later = SeedActivity(db, schedule.Id, "Reception", EventDateAt(20, 0), EventDateAt(21, 0), vendor.Id);
        var earlier = SeedActivity(db, schedule.Id, "Welcome", EventDateAt(18, 0), EventDateAt(18, 30), null);
        db.ScheduleConflicts.Add(new ScheduleConflict
        {
            ScheduleId = schedule.Id,
            ActivityId1 = earlier.Id,
            ActivityId2 = later.Id,
            ConflictType = ConflictDetectionService.VendorDoubleBooked,
            Description = "Vendor conflict.",
            IsResolved = false,
            DetectedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetByIdAsync(schedule.Id, CancellationToken.None);

        Assert.Equal("Launch Night", result.Event.EventTitle);
        Assert.Equal(planner.Email, result.Event.Planner?.Email);
        Assert.Equal([earlier.Id, later.Id], result.Activities.Select(activity => activity.Id).ToArray());
        Assert.Null(result.Activities[0].AssignedVendor);
        Assert.Equal(vendor.BusinessName, result.Activities[1].AssignedVendor?.BusinessName);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal(ConflictDetectionService.VendorDoubleBooked, conflict.ConflictType);
        Assert.False(conflict.IsResolved);
    }

    [Fact]
    public async Task GetByIdAsync_throws_for_missing_schedule()
    {
        using var db = CreateDb();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateService(db).GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public void AdminSchedulesController_is_admin_only()
    {
        var authorize = typeof(AdminSchedulesController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .ToList();

        Assert.Contains(authorize, attribute => attribute.Policy == "AdminOnly");
        Assert.Empty(typeof(AdminSchedulesController).GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
        Assert.Empty(typeof(AdminSchedulesController).GetMethods()
            .SelectMany(method => method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)));
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static AdminScheduleService CreateService(AppDbContext db) =>
        new(new AdminScheduleRepository(db));

    private static User SeedPlanner(AppDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "planner@example.com",
            FirstName = "Event",
            LastName = "Planner",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        return user;
    }

    private static Event SeedEvent(AppDbContext db, Guid ownerId, string name)
    {
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            EventName = name,
            EventType = EventType.CORPORATE,
            GuestCount = 100,
            Budget = 5000,
            PreferredVenue = "Main hall",
            PreferredDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(23, 0),
            EventDuration = TimeSpan.FromHours(5),
            Status = EventStatus.PLANNING,
            CreatedAt = DateTime.UtcNow
        };
        db.Events.Add(eventEntity);
        return eventEntity;
    }

    private static EventSchedule SeedSchedule(AppDbContext db, Guid eventId)
    {
        var schedule = new EventSchedule
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.EventSchedules.Add(schedule);
        return schedule;
    }

    private static Vendor SeedVendor(AppDbContext db)
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BusinessName = "Premier Catering",
            Category = BusinessCategory.CATERING,
            ContactEmail = "catering@example.com",
            ContactPhone = "0770000000",
            Address = "Vendor address",
            Status = VendorStatus.APPROVED,
            CreatedAt = DateTime.UtcNow
        };
        db.Vendors.Add(vendor);
        return vendor;
    }

    private static TimelineActivity SeedActivity(
        AppDbContext db,
        Guid scheduleId,
        string title,
        DateTime start,
        DateTime end,
        Guid? vendorId)
    {
        var activity = new TimelineActivity
        {
            Id = Guid.NewGuid(),
            ScheduleId = scheduleId,
            Title = title,
            StartTime = start,
            EndTime = end,
            AssignedVendorId = vendorId,
            Status = nameof(ActivityStatus.SCHEDULED),
            CreatedAt = DateTime.UtcNow
        };
        db.TimelineActivities.Add(activity);
        return activity;
    }

    private static DateTime EventDateAt(int hour, int minute) =>
        new(2026, 10, 15, hour, minute, 0, DateTimeKind.Utc);
}
