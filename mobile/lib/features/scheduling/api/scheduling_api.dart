import 'package:dio/dio.dart';

import '../../../core/api/dio_client.dart';
import '../models/event_schedule.dart';
import '../models/timeline_activity.dart';

class SchedulingApi {
  final Dio dio;

  SchedulingApi({Dio? dio}) : dio = dio ?? DioClient().dio;

  Future<EventSchedule> getSchedule(String eventId) async {
    final response = await dio.get('/api/schedules/event/$eventId');
    return EventSchedule.fromJson(response.data as Map<String, dynamic>);
  }

  Future<TimelineActivity> updateActivityStatus(String activityId, String status) async {
    try {
      final response = await dio.patch(
        '/api/schedules/activities/$activityId/status',
        data: {'status': status},
      );
      return TimelineActivity.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      final message = e.response?.data is Map
          ? (e.response?.data['message'] as String? ??
              'Unable to update the activity status.')
          : 'Unable to update the activity status.';
      throw Exception(message);
    }
  }
}
