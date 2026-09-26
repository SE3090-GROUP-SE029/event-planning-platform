import 'package:dio/dio.dart';
import '../../../core/api/dio_client.dart';
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
          'severity': severity.name[0].toUpperCase() + severity.name.substring(1),
        },
        options: _auth(token));
    return EventPlan.fromJson(_payload(response));
  }

  Future<EventPlan> regenerate(String token, String eventId) async {
    final response = await dio.post('/api/events/$eventId/plans/generate',
        options: _auth(token));
    return EventPlan.fromJson(_payload(response));
  }
}
