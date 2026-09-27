import 'timeline_activity.dart';
import 'schedule_conflict.dart';

class EventSchedule {
  final String id;
  final String eventId;
  final bool isLocked;
  final List<TimelineActivity> activities;
  final List<ScheduleConflict> conflicts;

  const EventSchedule({
    required this.id,
    required this.eventId,
    required this.isLocked,
    required this.activities,
    required this.conflicts,
  });

  factory EventSchedule.fromJson(Map<String, dynamic> json) {
    final rawActivities = json['activities'];
    final rawConflicts = json['conflicts'];

    final activities = (rawActivities is List)
        ? rawActivities
            .map((item) => TimelineActivity.fromJson(item as Map<String, dynamic>))
            .toList()
        : <TimelineActivity>[];

    final conflicts = (rawConflicts is List)
        ? rawConflicts
            .map((item) => ScheduleConflict.fromJson(item as Map<String, dynamic>))
            .toList()
        : <ScheduleConflict>[];

    return EventSchedule(
      id: (json['id'] ?? '').toString(),
      eventId: (json['eventId'] ?? '').toString(),
      isLocked: json['isLocked'] == true,
      activities: activities,
      conflicts: conflicts,
    );
  }
}
