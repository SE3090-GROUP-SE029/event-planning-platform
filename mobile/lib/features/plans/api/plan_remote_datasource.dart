import 'package:dio/dio.dart';
import '../../../core/api/dio_client.dart';
import '../models/plan_generation_job_model.dart';
import '../models/plan_model.dart';

class PlanRemoteDataSource {
  final Dio dio;
  PlanRemoteDataSource({Dio? dio}) : dio = dio ?? DioClient().dio;

  Options _auth(String token) =>
      Options(headers: {'Authorization': 'Bearer $token'});

  Map<String, dynamic> _payload(Response<dynamic> response) {
    final body = response.data as Map<String, dynamic>;
    return (body['data'] as Map<String, dynamic>?) ?? body;
  }

  Future<EventPlan> get(String token, String planId) async {
    final response = await dio.get('/api/plans/$planId', options: _auth(token));
    return EventPlan.fromJson(_payload(response));
  }

  Future<List<EventPlan>> listForEvent(String token, String eventId) async {
    final response =
        await dio.get('/api/events/$eventId/plans', options: _auth(token));
    final body = response.data as Map<String, dynamic>;
    final plans = body['data'] as List<dynamic>? ?? [];
    return plans
        .map((item) => EventPlan.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<EventPlan> approve(String token, String planId, String? notes) async {
    final response = await dio.post('/api/plans/$planId/approve',
        data: {'planId': planId, 'approverNotes': notes},
        options: _auth(token));
    return EventPlan.fromJson(_payload(response));
  }

  Future<EventPlan> reject(String token, String planId, String remarks,
      RejectionSeverity severity) async {
    final response = await dio.post('/api/plans/$planId/reject',
        data: {
          'planId': planId,
          'remarks': remarks,
          'severity':
              severity.name[0].toUpperCase() + severity.name.substring(1),
        },
        options: _auth(token));
    return EventPlan.fromJson(_payload(response));
  }

  Future<EventPlan> generate(String token, String eventId) async {
    final response = await dio.post(
      '/api/events/$eventId/plans/generate',
      options: _auth(token),
    );
    return _waitForPlan(token, PlanGenerationJob.fromJson(_payload(response)));
  }

  Future<EventPlan> regenerate(String token, String eventId,
      {required String reason}) async {
    final response = await dio.post(
      '/api/events/$eventId/plans/generate',
      data: {
        'eventId': eventId,
        'regenerate': true,
        'regenerationReason': reason,
      },
      options: _auth(token),
    );
    return _waitForPlan(token, PlanGenerationJob.fromJson(_payload(response)));
  }

  Future<PlanGenerationJob?> getLatestGeneration(
    String token,
    String eventId,
  ) async {
    final response = await dio.get(
      '/api/events/$eventId/plans/generation/latest',
      options: _auth(token),
    );
    final body = response.data;
    if (body is! Map<String, dynamic>) {
      throw const FormatException('Plan generation response must be an object.');
    }
    final data = body['data'];
    if (data == null) return null;
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Plan generation data must be an object.');
    }
    return PlanGenerationJob.fromJson(data);
  }

  Future<PlanGenerationJob> getGeneration(
    String token,
    String eventId,
    String jobId,
  ) async {
    final response = await dio.get(
      '/api/events/$eventId/plans/generation/$jobId',
      options: _auth(token),
    );
    return PlanGenerationJob.fromJson(_payload(response));
  }

  Future<EventPlan> waitForGeneration(
    String token,
    PlanGenerationJob job,
  ) =>
      _waitForPlan(token, job);

  Future<EventPlan> _waitForPlan(
    String token,
    PlanGenerationJob job,
  ) async {
    var current = job;
    while (true) {
      switch (current.status) {
        case PlanGenerationJobStatus.queued:
        case PlanGenerationJobStatus.processing:
          await Future<void>.delayed(const Duration(seconds: 2));
          current = await getGeneration(token, current.eventId, current.jobId);
          continue;
        case PlanGenerationJobStatus.succeeded:
          final planId = current.planId;
          if (planId == null || planId.isEmpty) {
            throw const FormatException(
              'Completed plan generation has no plan identifier.',
            );
          }
          return get(token, planId);
        case PlanGenerationJobStatus.failed:
          throw PlanGenerationFailedException(
            current.message ?? 'Plan generation failed. Please retry.',
          );
      }
    }
  }
}
