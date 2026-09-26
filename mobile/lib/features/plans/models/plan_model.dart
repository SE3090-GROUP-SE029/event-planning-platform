enum PlanStatus { draft, pendingPlannerReview, approved, rejected, superseded }

enum RejectionSeverity { minor, moderate, critical }

class PlanRisk {
  final String risk;
  final String severity;
  final String recommendation;

  const PlanRisk({
    required this.risk,
    required this.severity,
    required this.recommendation,
  });

  factory PlanRisk.fromJson(Map<String, dynamic> json) => PlanRisk(
        risk: json['risk'] as String? ?? 'Unspecified risk',
        severity: '${json['severity'] ?? 'medium'}',
        recommendation: json['recommendation'] as String? ?? '',
      );
}

class MissingRequirement {
  final String requirement;
  final String reason;
  final String category;

  const MissingRequirement({
    required this.requirement,
    required this.reason,
    required this.category,
  });

  factory MissingRequirement.fromJson(Map<String, dynamic> json) =>
      MissingRequirement(
        requirement: json['requirement'] as String? ?? '',
        reason: json['reason'] as String? ?? '',
        category: '${json['category'] ?? ''}',
      );
}

class EventSnapshot {
  final String eventName;
  final DateTime eventDate;
  final String location;
  final int guestCount;
  final double budget;

  const EventSnapshot({
    required this.eventName,
    required this.eventDate,
    required this.location,
    required this.guestCount,
    required this.budget,
  });

  factory EventSnapshot.fromJson(Map<String, dynamic> json) => EventSnapshot(
        eventName: json['eventName'] as String? ?? 'Unnamed event',
        eventDate: DateTime.tryParse('${json['eventDate']}') ?? DateTime.now(),
        location: json['location'] as String? ?? 'Not specified',
        guestCount: (json['guestCount'] as num?)?.toInt() ?? 0,
        budget: (json['budget'] as num?)?.toDouble() ?? 0,
      );
}

PlanStatus _statusFromJson(dynamic value) {
  if (value is int && value >= 0 && value < PlanStatus.values.length) {
    return PlanStatus.values[value];
  }
  final normalized = '${value ?? ''}'
      .replaceAll('_', '')
      .replaceAll(' ', '')
      .toLowerCase();
  return PlanStatus.values.firstWhere(
    (status) => status.name.toLowerCase() == normalized,
    orElse: () => PlanStatus.draft,
  );
}

class EventPlan {
  final String id;
  final String eventId;
  final int version;
  final PlanStatus status;
  final List<String> serviceCategories;
  final Map<String, double> budgetAllocation;
  final Map<String, String> proposedTimeline;
  final String rationale;
  final int completenessScore;
  final String validationSummary;
  final List<PlanRisk> risks;
  final List<MissingRequirement> missingRequirements;
  final EventSnapshot event;
  final String? plannerRemarks;

  const EventPlan({
    required this.id,
    required this.eventId,
    required this.version,
    required this.status,
    required this.serviceCategories,
    required this.budgetAllocation,
    required this.proposedTimeline,
    required this.rationale,
    required this.completenessScore,
    required this.validationSummary,
    required this.risks,
    required this.missingRequirements,
    required this.event,
    required this.plannerRemarks,
  });

  factory EventPlan.fromJson(Map<String, dynamic> json) => EventPlan(
        id: '${json['id'] ?? ''}',
        eventId: '${json['eventId'] ?? ''}',
        version: (json['version'] as num?)?.toInt() ?? 1,
        status: _statusFromJson(json['status']),
        serviceCategories: (json['serviceCategories'] as List<dynamic>? ?? [])
            .map((item) => '$item')
            .toList(),
        budgetAllocation: (json['budgetAllocation']
                    as Map<String, dynamic>? ??
                {})
            .map((key, value) => MapEntry(key, (value as num?)?.toDouble() ?? 0)),
        proposedTimeline:
            (json['proposedTimeline'] as Map<String, dynamic>? ?? {})
                .map((key, value) => MapEntry(key, '$value')),
        rationale: json['rationale'] as String? ?? '',
        completenessScore:
            (json['planCompletenessScore'] as num?)?.toInt() ?? 0,
        validationSummary: json['validationSummary'] as String? ?? '',
        risks: (json['identifiedRisks'] as List<dynamic>? ?? [])
            .map((item) => PlanRisk.fromJson(item as Map<String, dynamic>))
            .toList(),
        missingRequirements:
            (json['missingRequirements'] as List<dynamic>? ?? [])
                .map((item) =>
                    MissingRequirement.fromJson(item as Map<String, dynamic>))
                .toList(),
        event: EventSnapshot.fromJson(
            json['eventSnapshot'] as Map<String, dynamic>? ?? {}),
        plannerRemarks: json['plannerRemarks'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'eventId': eventId,
        'version': version,
        'status': status.name,
        'serviceCategories': serviceCategories,
        'budgetAllocation': budgetAllocation,
        'proposedTimeline': proposedTimeline,
        'rationale': rationale,
        'planCompletenessScore': completenessScore,
        'validationSummary': validationSummary,
        'identifiedRisks': risks
            .map((item) => {
                  'risk': item.risk,
                  'severity': item.severity,
                  'recommendation': item.recommendation,
                })
            .toList(),
        'missingRequirements': missingRequirements
            .map((item) => {
                  'requirement': item.requirement,
                  'reason': item.reason,
                  'category': item.category,
                })
            .toList(),
        'eventSnapshot': {
          'eventName': event.eventName,
          'eventDate': event.eventDate.toIso8601String(),
          'location': event.location,
          'guestCount': event.guestCount,
          'budget': event.budget,
        },
        'plannerRemarks': plannerRemarks,
      };
}
