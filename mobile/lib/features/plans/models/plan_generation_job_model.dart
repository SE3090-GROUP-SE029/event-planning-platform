enum PlanGenerationJobStatus { queued, processing, succeeded, failed }

class PlanGenerationJob {
  final String jobId;
  final String eventId;
  final PlanGenerationJobStatus status;
  final String? planId;
  final String? message;

  const PlanGenerationJob({
    required this.jobId,
    required this.eventId,
    required this.status,
    required this.planId,
    required this.message,
  });

  factory PlanGenerationJob.fromJson(Map<String, dynamic> json) {
    final normalizedStatus = '${json['status'] ?? ''}'
        .replaceAll(RegExp(r'[_\s-]'), '')
        .toLowerCase();
    final status = PlanGenerationJobStatus.values.firstWhere(
      (value) => value.name.toLowerCase() == normalizedStatus,
      orElse: () => throw FormatException(
        'Unknown plan generation status: ${json['status']}',
      ),
    );
    return PlanGenerationJob(
      jobId: '${json['jobId'] ?? ''}',
      eventId: '${json['eventId'] ?? ''}',
      status: status,
      planId: json['planId'] as String?,
      message: json['message'] as String?,
    );
  }
}

class PlanGenerationFailedException implements Exception {
  final String message;

  const PlanGenerationFailedException(this.message);

  @override
  String toString() => message;
}
