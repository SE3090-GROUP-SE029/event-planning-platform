import 'timeline_activity.dart';
import 'schedule_conflict.dart';

class EventSchedule {
  final String id;
  final String eventId;
  final String? eventName;
  final DateTime? eventDate;
  final Duration? eventStartTime;
  final Duration? eventEndTime;
  final bool isLocked;
  final List<TimelineActivity> activities;
  final List<ScheduleConflict> conflicts;

  const EventSchedule({
    required this.id,
    required this.eventId,
    this.eventName,
    this.eventDate,
    this.eventStartTime,
    this.eventEndTime,
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
      eventName: json['eventName']?.toString(),
      eventDate: _parseDate(json['eventDate']),
      eventStartTime: _parseTime(json['eventStartTime']),
      eventEndTime: _parseTime(json['eventEndTime']),
      isLocked: json['isLocked'] == true,
      activities: activities,
      conflicts: conflicts,
    );
  }

  EventSchedule copyWith({
    String? id,
    String? eventId,
    String? eventName,
    DateTime? eventDate,
    Duration? eventStartTime,
    Duration? eventEndTime,
    bool? isLocked,
    List<TimelineActivity>? activities,
    List<ScheduleConflict>? conflicts,
  }) {
    return EventSchedule(
      id: id ?? this.id,
      eventId: eventId ?? this.eventId,
      eventName: eventName ?? this.eventName,
      eventDate: eventDate ?? this.eventDate,
      eventStartTime: eventStartTime ?? this.eventStartTime,
      eventEndTime: eventEndTime ?? this.eventEndTime,
      isLocked: isLocked ?? this.isLocked,
      activities: activities ?? this.activities,
      conflicts: conflicts ?? this.conflicts,
    );
  }
}

DateTime? _parseDate(dynamic value) {
  if (value == null) return null;
  if (value is DateTime) return value;
  final text = value.toString();
  if (text.isEmpty) return null;
  return DateTime.tryParse(text);
}

Duration? _parseTime(dynamic value) {
  if (value == null) return null;
  if (value is num) return Duration(microseconds: value.toInt());
  final parts = value.toString().split(':');
  if (parts.length < 2) return null;
  return Duration(
    hours: int.tryParse(parts[0]) ?? 0,
    minutes: int.tryParse(parts[1]) ?? 0,
    seconds: parts.length > 2 ? int.tryParse(parts[2].split('.').first) ?? 0 : 0,
  );
}
