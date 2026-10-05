using Application.Services.Scheduling;
using Domain.Entities;
using Xunit;

namespace Backend.UnitTests.Services;

public class ConflictDetectionServiceTests
{
    private readonly ConflictDetectionService _conflictDetectionService;

    public ConflictDetectionServiceTests()
    {
        _conflictDetectionService = new ConflictDetectionService();
    }

    [Fact]
    public void DetectConflicts_OverlappingTimesWithSameVendor_DetectsVendorDoubleBooked()
    {
        var scheduleId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddHours(13);

        var activities = new List<TimelineActivity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId,
                Title = "Stage Sound Check",
                StartTime = baseTime,
                EndTime = baseTime.AddMinutes(90),
                AssignedVendorId = vendorId
            },
            new()
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId,
                Title = "Acoustic Performance",
                StartTime = baseTime.AddMinutes(45),
                EndTime = baseTime.AddMinutes(105),
                AssignedVendorId = vendorId
            }
        };

        var conflicts = _conflictDetectionService.DetectConflicts(scheduleId, activities);

        var conflict = Assert.Single(conflicts);
        Assert.Equal(ConflictDetectionService.VendorDoubleBooked, conflict.ConflictType);
    }

    [Fact]
    public void DetectConflicts_OverlappingTimesDifferentVendors_DetectsActivityOverlap()
    {
        var scheduleId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddHours(18);

        var activities = new List<TimelineActivity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId,
                Title = "Ceremony",
                StartTime = baseTime,
                EndTime = baseTime.AddHours(1),
                AssignedVendorId = Guid.NewGuid()
            },
            new()
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId,
                Title = "Dinner Setup",
                StartTime = baseTime.AddMinutes(30),
                EndTime = baseTime.AddMinutes(90),
                AssignedVendorId = Guid.NewGuid()
            }
        };

        var conflicts = _conflictDetectionService.DetectConflicts(scheduleId, activities);

        var conflict = Assert.Single(conflicts);
        Assert.Equal(ConflictDetectionService.ActivityOverlap, conflict.ConflictType);
        Assert.Equal("Schedule overlap: Ceremony overlaps with Dinner Setup from 6:30 PM to 7:00 PM.", conflict.Description);
    }

    [Fact]
    public void DetectConflicts_OverlappingTimesNoVendors_DetectsActivityOverlap()
    {
        var scheduleId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddHours(18);

        var activities = new List<TimelineActivity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId,
                Title = "Ceremony",
                StartTime = baseTime,
                EndTime = baseTime.AddHours(1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId,
                Title = "Dinner Setup",
                StartTime = baseTime.AddMinutes(30),
                EndTime = baseTime.AddMinutes(90)
            }
        };

        var conflicts = _conflictDetectionService.DetectConflicts(scheduleId, activities);

        var conflict = Assert.Single(conflicts);
        Assert.Equal(ConflictDetectionService.ActivityOverlap, conflict.ConflictType);
    }

    [Fact]
    public void DetectConflicts_ExactSameTimeRange_DetectsConflict()
    {
        var scheduleId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddHours(18);

        var conflicts = _conflictDetectionService.DetectConflicts(scheduleId, new List<TimelineActivity>
        {
            Activity(scheduleId, "A", baseTime, baseTime.AddHours(1)),
            Activity(scheduleId, "B", baseTime, baseTime.AddHours(1))
        });

        Assert.Single(conflicts);
    }

    [Fact]
    public void DetectConflicts_ActivityFullyInsideAnother_DetectsConflict()
    {
        var scheduleId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddHours(18);

        var conflicts = _conflictDetectionService.DetectConflicts(scheduleId, new List<TimelineActivity>
        {
            Activity(scheduleId, "A", baseTime, baseTime.AddHours(2)),
            Activity(scheduleId, "B", baseTime.AddMinutes(30), baseTime.AddMinutes(60))
        });

        Assert.Single(conflicts);
    }

    [Fact]
    public void DetectConflicts_EndTimeEqualToNextStartTime_ReturnsNoConflicts()
    {
        var scheduleId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddHours(18);

        var conflicts = _conflictDetectionService.DetectConflicts(scheduleId, new List<TimelineActivity>
        {
            Activity(scheduleId, "A", baseTime, baseTime.AddHours(1)),
            Activity(scheduleId, "B", baseTime.AddHours(1), baseTime.AddHours(2))
        });

        Assert.Empty(conflicts);
    }

    [Fact]
    public void DetectConflicts_NonOverlappingActivities_ReturnsNoConflicts()
    {
        var scheduleId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddHours(18);

        var conflicts = _conflictDetectionService.DetectConflicts(scheduleId, new List<TimelineActivity>
        {
            Activity(scheduleId, "A", baseTime, baseTime.AddMinutes(30)),
            Activity(scheduleId, "B", baseTime.AddHours(1), baseTime.AddHours(2))
        });

        Assert.Empty(conflicts);
    }

    [Fact]
    public void DetectConflicts_EmptyList_ReturnsEmpty()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var activities = new List<TimelineActivity>();

        // Act
        var conflicts = _conflictDetectionService.DetectConflicts(scheduleId, activities);

        // Assert
        Assert.NotNull(conflicts);
        Assert.Empty(conflicts);
    }

    private static TimelineActivity Activity(
        Guid scheduleId,
        string title,
        DateTime startTime,
        DateTime endTime,
        Guid? assignedVendorId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ScheduleId = scheduleId,
            Title = title,
            StartTime = startTime,
            EndTime = endTime,
            AssignedVendorId = assignedVendorId
        };
}
