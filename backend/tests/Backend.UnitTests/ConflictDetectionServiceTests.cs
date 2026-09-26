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
    public void DetectConflicts_SequentialActivities_ReturnsNoConflicts()
    {
        // Arrange: Activity B starts exactly when Activity A ends
        var scheduleId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.Date.AddHours(9);

        var activities = new List<TimelineActivity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId,
                Title = "Morning Keynote",
                StartTime = startTime,
                EndTime = startTime.AddHours(1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId,
                Title = "Panel Discussion",
                StartTime = startTime.AddHours(1),
                EndTime = startTime.AddHours(2)
            }
        };

        // Act
        var conflicts = _conflictDetectionService.DetectConflicts(scheduleId, activities);

        // Assert
        Assert.NotNull(conflicts);
        Assert.Empty(conflicts);
    }

    [Fact]
    public void DetectConflicts_OverlappingTimesWithSameVendor_DetectsConflict()
    {
        // Arrange: Same vendor booked for overlapping time windows
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
                StartTime = baseTime.AddMinutes(45), // Overlaps by 45 minutes
                EndTime = baseTime.AddMinutes(105),
                AssignedVendorId = vendorId
            }
        };

        // Act
        var conflicts = _conflictDetectionService.DetectConflicts(scheduleId, activities);

        // Assert
        Assert.NotNull(conflicts);
        Assert.Single(conflicts);
    }

    [Fact]
    public void DetectConflicts_OverlappingTimesDifferentVendors_ReturnsNoVendorConflict()
    {
        // Arrange: Overlapping times, but different vendors (or no vendor)
        var scheduleId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddHours(15);

        var activities = new List<TimelineActivity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId,
                Title = "Hall A Workshop",
                StartTime = baseTime,
                EndTime = baseTime.AddHours(1),
                AssignedVendorId = Guid.NewGuid()
            },
            new()
            {
                Id = Guid.NewGuid(),
                ScheduleId = scheduleId,
                Title = "Hall B Workshop",
                StartTime = baseTime.AddMinutes(15),
                EndTime = baseTime.AddMinutes(45),
                AssignedVendorId = Guid.NewGuid()
            }
        };

        // Act
        var conflicts = _conflictDetectionService.DetectConflicts(scheduleId, activities);

        // Assert
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
}