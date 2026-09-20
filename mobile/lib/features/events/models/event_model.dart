enum EventType { wedding, corporate, birthday, anniversary, other }

enum EventStatus {
  draft,
  planning,
  inPlanning,
  confirmed,
  completed,
  cancelled,
}

String _normalizeEnumValue(String value) =>
    value.replaceAll('_', '').replaceAll('-', '').toLowerCase();

String _enumName(dynamic value) => value.toString().split('.').last;

T _parseEnum<T>(dynamic value, List<T> values) {
  if (value is int && value >= 0 && value < values.length) return values[value];
  final targetName = value?.toString();
  if (targetName == null || targetName.isEmpty) return values.first;

  final normalizedTarget = _normalizeEnumValue(targetName);
  return values.firstWhere(
    (item) => _normalizeEnumValue(_enumName(item)) == normalizedTarget,
    orElse: () => values.first,
  );
}

Duration _parseDuration(dynamic value) {
  if (value is num) return Duration(microseconds: value.toInt());
  final parts = (value?.toString() ?? '').split(':');
  if (parts.length == 3) {
    return Duration(
      hours: int.tryParse(parts[0]) ?? 0,
      minutes: int.tryParse(parts[1]) ?? 0,
      seconds: int.tryParse(parts[2].split('.').first) ?? 0,
    );
  }
  return Duration.zero;
}

String _durationJson(Duration value) {
  final hours = value.inHours.toString().padLeft(2, '0');
  final minutes = (value.inMinutes % 60).toString().padLeft(2, '0');
  final seconds = (value.inSeconds % 60).toString().padLeft(2, '0');
  return '$hours:$minutes:$seconds';
}

class EventModel {
  final String id;
  final String ownerId;
  final EventType eventType;
  final int guestCount;
  final double budget;
  final String preferredVenue;
  final DateTime preferredDate;
  final Duration eventDuration;
  final String? requirements;
  final EventStatus status;
  final DateTime createdAt;
  final DateTime? updatedAt;

  const EventModel({
    required this.id,
    required this.ownerId,
    required this.eventType,
    required this.guestCount,
    required this.budget,
    required this.preferredVenue,
    required this.preferredDate,
    required this.eventDuration,
    required this.requirements,
    required this.status,
    required this.createdAt,
    required this.updatedAt,
  });

  factory EventModel.fromJson(Map<String, dynamic> json) => EventModel(
        id: '${json['id'] ?? ''}',
        ownerId: '${json['ownerId'] ?? ''}',
        eventType: _parseEnum(json['eventType'], EventType.values),
        guestCount: (json['guestCount'] as num?)?.toInt() ?? 0,
        budget: (json['budget'] as num?)?.toDouble() ?? 0,
        preferredVenue: json['preferredVenue'] as String? ?? '',
        preferredDate: DateTime.parse(json['preferredDate'] as String),
        eventDuration: _parseDuration(json['eventDuration']),
        requirements: json['requirements'] as String?,
        status: _parseEnum(json['status'], EventStatus.values),
        createdAt: DateTime.parse(json['createdAt'] as String),
        updatedAt: json['updatedAt'] == null
            ? null
            : DateTime.parse(json['updatedAt'] as String),
      );

  Map<String, dynamic> toCreateJson() => {
        'eventType': eventType.index,
        'guestCount': guestCount,
        'budget': budget,
        'preferredVenue': preferredVenue,
        'preferredDate': preferredDate.toUtc().toIso8601String(),
        'eventDuration': _durationJson(eventDuration),
        'requirements': requirements,
      };

  Map<String, dynamic> toUpdateJson() => {
        'eventType': eventType.index,
        'guestCount': guestCount,
        'budget': budget,
        'preferredVenue': preferredVenue,
        'preferredDate': preferredDate.toUtc().toIso8601String(),
        'requirements': requirements,
        'status': status.index,
      };
}

class EventPage {
  final List<EventModel> items;
  final int page;
  final int pageSize;
  final int totalCount;
  final int totalPages;

  const EventPage({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.totalPages,
  });

  factory EventPage.fromJson(Map<String, dynamic> json) => EventPage(
        items: (json['items'] as List<dynamic>? ?? [])
            .map((item) => EventModel.fromJson(item as Map<String, dynamic>))
            .toList(),
        page: (json['page'] as num?)?.toInt() ?? 1,
        pageSize: (json['pageSize'] as num?)?.toInt() ?? 10,
        totalCount: (json['totalCount'] as num?)?.toInt() ?? 0,
        totalPages: (json['totalPages'] as num?)?.toInt() ?? 0,
      );
}
