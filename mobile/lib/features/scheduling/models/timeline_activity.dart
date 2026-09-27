class TimelineActivity {
  final String id;
  final String scheduleId;
  final String title;
  final String? description;
  final DateTime startTime;
  final DateTime endTime;
  final String? assignedVendorId;
  final String status;
  final DateTime createdAt;

  const TimelineActivity({
    required this.id,
    required this.scheduleId,
    required this.title,
    this.description,
    required this.startTime,
    required this.endTime,
    this.assignedVendorId,
    required this.status,
    required this.createdAt,
  });

  factory TimelineActivity.fromJson(Map<String, dynamic> json) {
    DateTime parseDateTime(dynamic value) {
      if (value == null) return DateTime.now();
      if (value is DateTime) return value;
      if (value is String) {
        final parsed = DateTime.tryParse(value);
        if (parsed != null) return parsed;
      }
      if (value is int) {
        return DateTime.fromMillisecondsSinceEpoch(value);
      }
      return DateTime.now();
    }

    return TimelineActivity(
      id: (json['id'] ?? '').toString(),
      scheduleId: (json['scheduleId'] ?? '').toString(),
      title: (json['title'] ?? '').toString(),
      description: json['description']?.toString(),
      startTime: parseDateTime(json['startTime']),
      endTime: parseDateTime(json['endTime']),
      assignedVendorId: json['assignedVendorId']?.toString(),
      status: (json['status'] ?? '').toString(),
      createdAt: parseDateTime(json['createdAt']),
    );
  }

  TimelineActivity copyWith({
    String? id,
    String? scheduleId,
    String? title,
    String? description,
    DateTime? startTime,
    DateTime? endTime,
    String? assignedVendorId,
    String? status,
    DateTime? createdAt,
  }) {
    return TimelineActivity(
      id: id ?? this.id,
      scheduleId: scheduleId ?? this.scheduleId,
      title: title ?? this.title,
      description: description ?? this.description,
      startTime: startTime ?? this.startTime,
      endTime: endTime ?? this.endTime,
      assignedVendorId: assignedVendorId ?? this.assignedVendorId,
      status: status ?? this.status,
      createdAt: createdAt ?? this.createdAt,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'scheduleId': scheduleId,
      'title': title,
      'description': description,
      'startTime': startTime.toIso8601String(),
      'endTime': endTime.toIso8601String(),
      'assignedVendorId': assignedVendorId,
      'status': status,
      'createdAt': createdAt.toIso8601String(),
    };
  }
}
