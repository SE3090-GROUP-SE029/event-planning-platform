class ScheduleConflict {
  final String id;
  final String scheduleId;
  final String? activityId1;
  final String? activityId2;
  final String conflictType;
  final String description;
  final bool isResolved;

  const ScheduleConflict({
    required this.id,
    required this.scheduleId,
    this.activityId1,
    this.activityId2,
    required this.conflictType,
    required this.description,
    required this.isResolved,
  });

  factory ScheduleConflict.fromJson(Map<String, dynamic> json) {
    return ScheduleConflict(
      id: (json['id'] ?? '').toString(),
      scheduleId: (json['scheduleId'] ?? '').toString(),
      activityId1: json['activityId1']?.toString(),
      activityId2: json['activityId2']?.toString(),
      conflictType: (json['conflictType'] ?? '').toString(),
      description: (json['description'] ?? '').toString(),
      isResolved: json['isResolved'] == true,
    );
  }
}
