using System.Reflection;
using Api.Controllers;
using Application.Services.Scheduling;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class ScheduleAuthorizationTests
{
    [Fact]
    public async Task PlannerCanAccessOwnSchedule()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var eventEntity = SeedEvent(db, plannerId);
        var schedule = SeedSchedule(db, eventEntity.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetOrCreateScheduleForPlannerAsync(eventEntity.Id, plannerId);

        Assert.Equal(schedule.Id, result.Id);
    }

    [Fact]
    public async Task PlannerCannotAccessAnotherPlannersSchedule()
    {
        using var db = CreateDb();
        var eventEntity = SeedEvent(db, Guid.NewGuid());
        SeedSchedule(db, eventEntity.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetOrCreateScheduleForPlannerAsync(eventEntity.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task PlannerCannotModifyAnotherPlannersActivity()
    {
        using var db = CreateDb();
        var eventEntity = SeedEvent(db, Guid.NewGuid());
        var schedule = SeedSchedule(db, eventEntity.Id);
        var activity = SeedActivity(db, schedule.Id, assignedVendorId: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateActivityStatusForPlannerAsync(
                activity.Id,
                Guid.NewGuid(),
                ActivityStatus.COMPLETED));
    }

    [Fact]
    public async Task PlannerCannotGenerateAiScheduleForAnotherPlannersEvent()
    {
        using var db = CreateDb();
        var eventEntity = SeedEvent(db, Guid.NewGuid());
        var schedule = SeedSchedule(db, eventEntity.Id);
        await db.SaveChangesAsync();
        var aiClient = new FakeScheduleAiClient();
        var service = CreateService(db, aiClient);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GenerateScheduleWithAiForPlannerAsync(schedule.Id, Guid.NewGuid()));
        Assert.False(aiClient.WasCalled);
    }

    [Fact]
    public async Task VendorCanRetrieveOnlyAssignedActivities()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var eventEntity = SeedEvent(db, plannerId);
        var schedule = SeedSchedule(db, eventEntity.Id);
        var vendor = SeedVendor(db);
        var otherVendor = SeedVendor(db);
        var assigned = SeedActivity(db, schedule.Id, vendor.Id, "Assigned");
        SeedActivity(db, schedule.Id, otherVendor.Id, "Other vendor");
        SeedActivity(db, schedule.Id, assignedVendorId: null, "Unassigned");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetAssignedActivitiesForVendorAsync(vendor.UserId);

        var item = Assert.Single(result);
        Assert.Equal(assigned.Id, item.Id);
        Assert.Equal(schedule.Id, item.ScheduleId);
        Assert.Equal(eventEntity.Id, item.EventId);
    }

    [Fact]
    public async Task VendorCanUpdateAssignedActivity()
    {
        using var db = CreateDb();
        var eventEntity = SeedEvent(db, Guid.NewGuid());
        var schedule = SeedSchedule(db, eventEntity.Id);
        var vendor = SeedVendor(db);
        var activity = SeedActivity(db, schedule.Id, vendor.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateActivityStatusForVendorAsync(
            activity.Id,
            vendor.UserId,
            ActivityStatus.COMPLETED);

        Assert.NotNull(result);
        Assert.Equal(nameof(ActivityStatus.COMPLETED), result!.Status);
    }

    [Fact]
    public async Task VendorCannotUpdateAnotherVendorsActivity()
    {
        using var db = CreateDb();
        var eventEntity = SeedEvent(db, Guid.NewGuid());
        var schedule = SeedSchedule(db, eventEntity.Id);
        var assignedVendor = SeedVendor(db);
        var callerVendor = SeedVendor(db);
        var activity = SeedActivity(db, schedule.Id, assignedVendor.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateActivityStatusForVendorAsync(
                activity.Id,
                callerVendor.UserId,
                ActivityStatus.COMPLETED));
    }

    [Fact]
    public async Task PlannerSuccessfullyEditsOwnedActivity()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var eventEntity = SeedEvent(db, plannerId);
        var schedule = SeedSchedule(db, eventEntity.Id);
        var vendor = SeedVendor(db);
        var activity = SeedActivity(db, schedule.Id, assignedVendorId: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var start = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(1);

        var result = await service.UpdateActivityForPlannerAsync(
            activity.Id,
            plannerId,
            "Updated activity",
            "Updated notes",
            start,
            end,
            vendor.Id);

        Assert.NotNull(result);
        Assert.Equal("Updated activity", result!.Title);
        Assert.Equal("Updated notes", result.Description);
        Assert.Equal(start, result.StartTime);
        Assert.Equal(end, result.EndTime);
        Assert.Equal(vendor.Id, result.AssignedVendorId);
    }

    [Fact]
    public async Task PlannerSuccessfullyDeletesOwnedActivity()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var eventEntity = SeedEvent(db, plannerId);
        var schedule = SeedSchedule(db, eventEntity.Id);
        var activity = SeedActivity(db, schedule.Id, assignedVendorId: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.DeleteActivityForPlannerAsync(activity.Id, plannerId);

        Assert.True(result);
        Assert.Empty(db.TimelineActivities);
    }

    [Fact]
    public async Task AnotherPlannerCannotEditOrDeleteActivity()
    {
        using var db = CreateDb();
        var eventEntity = SeedEvent(db, Guid.NewGuid());
        var schedule = SeedSchedule(db, eventEntity.Id);
        var activity = SeedActivity(db, schedule.Id, assignedVendorId: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateActivityForPlannerAsync(
                activity.Id,
                Guid.NewGuid(),
                "Blocked edit",
                null,
                activity.StartTime,
                activity.EndTime,
                null));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteActivityForPlannerAsync(activity.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task InvalidActivityTimesAreRejected()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var eventEntity = SeedEvent(db, plannerId);
        var schedule = SeedSchedule(db, eventEntity.Id);
        var activity = SeedActivity(db, schedule.Id, assignedVendorId: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateActivityForPlannerAsync(
                activity.Id,
                plannerId,
                "Invalid time",
                null,
                activity.StartTime,
                activity.StartTime,
                null));
    }

    [Fact]
    public async Task NonexistentActivityIdsReturnNull()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var update = await service.UpdateActivityForPlannerAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Missing",
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            null);
        var delete = await service.DeleteActivityForPlannerAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(update);
        Assert.Null(delete);
    }

    [Fact]
    public async Task SameVendorOverlappingActivitiesProduceConflicts()
    {
        using var db = CreateDb();
        var (service, plannerId, schedule, vendor) = await SeedScheduleWithVendorAsync(db);
        var start = new DateTime(2026, 10, 15, 18, 0, 0, DateTimeKind.Utc);

        await service.AddActivityForPlannerAsync(schedule.Id, plannerId, "A", null, start, start.AddHours(1), vendor.Id);
        await service.AddActivityForPlannerAsync(schedule.Id, plannerId, "B", null, start.AddMinutes(30), start.AddMinutes(90), vendor.Id);

        Assert.Equal(1, CountUnresolvedVendorConflicts(db, schedule.Id));
    }

    [Fact]
    public async Task EditingActivityToNonOverlappingTimeRemovesStaleUnresolvedConflict()
    {
        using var db = CreateDb();
        var (service, plannerId, schedule, vendor) = await SeedScheduleWithVendorAsync(db);
        var start = new DateTime(2026, 10, 15, 18, 0, 0, DateTimeKind.Utc);
        await service.AddActivityForPlannerAsync(schedule.Id, plannerId, "A", null, start, start.AddHours(1), vendor.Id);
        var second = await service.AddActivityForPlannerAsync(schedule.Id, plannerId, "B", null, start.AddMinutes(30), start.AddMinutes(90), vendor.Id);
        Assert.Equal(1, CountUnresolvedVendorConflicts(db, schedule.Id));

        await service.UpdateActivityForPlannerAsync(
            second.Id,
            plannerId,
            second.Title,
            second.Description,
            start.AddHours(1),
            start.AddHours(2),
            vendor.Id);

        Assert.Equal(0, CountUnresolvedVendorConflicts(db, schedule.Id));
    }

    [Fact]
    public async Task DeletingOverlappingActivityRecalculatesConflicts()
    {
        using var db = CreateDb();
        var (service, plannerId, schedule, vendor) = await SeedScheduleWithVendorAsync(db);
        var start = new DateTime(2026, 10, 15, 18, 0, 0, DateTimeKind.Utc);
        await service.AddActivityForPlannerAsync(schedule.Id, plannerId, "A", null, start, start.AddHours(1), vendor.Id);
        var second = await service.AddActivityForPlannerAsync(schedule.Id, plannerId, "B", null, start.AddMinutes(30), start.AddMinutes(90), vendor.Id);
        Assert.Equal(1, CountUnresolvedVendorConflicts(db, schedule.Id));

        await service.DeleteActivityForPlannerAsync(second.Id, plannerId);

        Assert.Equal(0, CountUnresolvedVendorConflicts(db, schedule.Id));
    }

    [Fact]
    public async Task ConflictRecalculationDoesNotProduceDuplicateUnresolvedConflicts()
    {
        using var db = CreateDb();
        var (service, plannerId, schedule, vendor) = await SeedScheduleWithVendorAsync(db);
        var start = new DateTime(2026, 10, 15, 18, 0, 0, DateTimeKind.Utc);
        await service.AddActivityForPlannerAsync(schedule.Id, plannerId, "A", null, start, start.AddHours(1), vendor.Id);
        var second = await service.AddActivityForPlannerAsync(schedule.Id, plannerId, "B", null, start.AddMinutes(30), start.AddMinutes(90), vendor.Id);

        await service.UpdateActivityForPlannerAsync(
            second.Id,
            plannerId,
            second.Title,
            second.Description,
            start.AddMinutes(30),
            start.AddMinutes(90),
            vendor.Id);

        Assert.Equal(1, CountUnresolvedVendorConflicts(db, schedule.Id));
    }

    [Fact]
    public void VendorCannotPerformPlannerOnlyOperationsByEndpointPolicy()
    {
        AssertPolicy(nameof(SchedulesController.GetSchedule), "EventPlannerOnly");
        AssertPolicy(nameof(SchedulesController.AddActivity), "EventPlannerOnly");
        AssertPolicy(nameof(SchedulesController.UpdateActivity), "EventPlannerOnly");
        AssertPolicy(nameof(SchedulesController.DeleteActivity), "EventPlannerOnly");
        AssertPolicy(nameof(SchedulesController.GenerateAiSchedule), "EventPlannerOnly");
        AssertPolicy(nameof(SchedulesController.GetMyVendorActivities), "VendorOnly");
    }

    [Fact]
    public void SchedulesControllerRequiresAuthenticatedRequests()
    {
        var authorize = typeof(SchedulesController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .ToList();

        Assert.NotEmpty(authorize);
        Assert.Empty(typeof(SchedulesController).GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
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

    private static ScheduleService CreateService(
        AppDbContext db,
        IScheduleAiClient? aiClient = null) =>
        new(
            new ScheduleRepository(db),
            new ConflictDetectionService(),
            new EventRepository(db),
            aiClient ?? new FakeScheduleAiClient(),
            new VendorRepository(db));

    private static Event SeedEvent(AppDbContext db, Guid ownerId)
    {
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            EventName = "C3 authorization event",
            EventType = EventType.CORPORATE,
            GuestCount = 50,
            Budget = 2500,
            PreferredVenue = "Hall",
            PreferredDate = new DateTime(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc),
            EventDuration = TimeSpan.FromHours(4),
            Status = EventStatus.DRAFT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
            BusinessName = "Vendor",
            Category = BusinessCategory.PHOTOGRAPHY,
            ContactEmail = $"{Guid.NewGuid():N}@vendor.test",
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
        Guid? assignedVendorId,
        string title = "Timeline activity")
    {
        var activity = new TimelineActivity
        {
            Id = Guid.NewGuid(),
            ScheduleId = scheduleId,
            Title = title,
            StartTime = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 10, 15, 11, 0, 0, DateTimeKind.Utc),
            AssignedVendorId = assignedVendorId,
            Status = nameof(ActivityStatus.SCHEDULED),
            CreatedAt = DateTime.UtcNow
        };
        db.TimelineActivities.Add(activity);
        return activity;
    }

    private static async Task<(ScheduleService Service, Guid PlannerId, EventSchedule Schedule, Vendor Vendor)>
        SeedScheduleWithVendorAsync(AppDbContext db)
    {
        var plannerId = Guid.NewGuid();
        var eventEntity = SeedEvent(db, plannerId);
        var schedule = SeedSchedule(db, eventEntity.Id);
        var vendor = SeedVendor(db);
        await db.SaveChangesAsync();
        return (CreateService(db), plannerId, schedule, vendor);
    }

    private static int CountUnresolvedVendorConflicts(AppDbContext db, Guid scheduleId) =>
        db.ScheduleConflicts.Count(conflict =>
            conflict.ScheduleId == scheduleId
            && !conflict.IsResolved
            && conflict.ConflictType == "VendorDoubleBooked");

    private static void AssertPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(SchedulesController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Method {methodName} was not found.");
        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();

        Assert.Contains(authorize, attribute => attribute.Policy == expectedPolicy);
    }

    private sealed class FakeScheduleAiClient : IScheduleAiClient
    {
        public bool WasCalled { get; private set; }

        public Task<AiScheduleGenerationResult> GenerateScheduleAsync(
            Event eventEntity,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new AiScheduleGenerationResult
            {
                Activities =
                [
                    new AiScheduleActivity
                    {
                        Title = "Generated setup",
                        Description = "AI generated",
                        StartTime = "2026-10-15T09:00:00Z",
                        EndTime = "2026-10-15T10:00:00Z"
                    }
                ]
            });
        }
    }
}
