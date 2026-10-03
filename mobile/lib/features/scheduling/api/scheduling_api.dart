import 'package:dio/dio.dart';

import '../../../core/api/dio_client.dart';
import '../models/event_schedule.dart';
import '../models/schedule_conflict.dart';
import '../models/timeline_activity.dart';

class SchedulingApi {
  final Dio dio;

  SchedulingApi({Dio? dio}) : dio = dio ?? DioClient().dio;

  Future<EventSchedule> getSchedule(String eventId) async {
    final response = await dio.get('/api/schedules/event/$eventId');
    return EventSchedule.fromJson(response.data as Map<String, dynamic>);
  }

  Future<List<ScheduleConflict>> getConflicts(String scheduleId) async {
    try {
      final response = await dio.get('/api/schedules/$scheduleId/conflicts');
      final data = response.data as List<dynamic>;
      return data
          .map((item) => ScheduleConflict.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw Exception(_messageFrom(e, 'Unable to load schedule conflicts.'));
    }
  }

  Future<TimelineActivity> addActivity(
    String scheduleId, {
    required String title,
    String? description,
    required DateTime startTime,
    required DateTime endTime,
    String? assignedVendorId,
  }) async {
    try {
      final response = await dio.post(
        '/api/schedules/$scheduleId/activities',
        data: _activityPayload(
          title: title,
          description: description,
          startTime: startTime,
          endTime: endTime,
          assignedVendorId: assignedVendorId,
        ),
      );
      return TimelineActivity.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw Exception(_messageFrom(e, 'Unable to add the activity.'));
    }
  }

  Future<TimelineActivity> updateActivity(
    String activityId, {
    required String title,
    String? description,
    required DateTime startTime,
    required DateTime endTime,
    String? assignedVendorId,
  }) async {
    try {
      final response = await dio.put(
        '/api/schedules/activities/$activityId',
        data: _activityPayload(
          title: title,
          description: description,
          startTime: startTime,
          endTime: endTime,
          assignedVendorId: assignedVendorId,
        ),
      );
      return TimelineActivity.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw Exception(_messageFrom(e, 'Unable to update the activity.'));
    }
  }

  Future<void> deleteActivity(String activityId) async {
    try {
      await dio.delete('/api/schedules/activities/$activityId');
    } on DioException catch (e) {
      throw Exception(_messageFrom(e, 'Unable to delete the activity.'));
    }
  }

  Future<TimelineActivity> updateActivityStatus(String activityId, String status) async {
    try {
      final response = await dio.patch(
        '/api/schedules/activities/$activityId/status',
        data: {'status': _statusValue(status)},
      );
      return TimelineActivity.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw Exception(
        _messageFrom(e, 'Unable to update the activity status.'),
      );
    }
  }

  Future<EventSchedule> generateAiSchedule(String scheduleId) async {
    try {
      final response = await dio.post('/api/schedules/$scheduleId/generate-ai');
      return EventSchedule.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw Exception(
        _messageFrom(
          e,
          'We could not generate the schedule with AI right now.',
        ),
      );
    }
  }

  Map<String, dynamic> _activityPayload({
    required String title,
    String? description,
    required DateTime startTime,
    required DateTime endTime,
    String? assignedVendorId,
  }) {
    return {
      'title': title.trim(),
      'description': description?.trim().isEmpty == true
          ? null
          : description?.trim(),
      'startTime': startTime.toUtc().toIso8601String(),
      'endTime': endTime.toUtc().toIso8601String(),
      'assignedVendorId': assignedVendorId?.trim().isEmpty == true
          ? null
          : assignedVendorId?.trim(),
    };
  }

  String _messageFrom(DioException error, String fallback) {
    final data = error.response?.data;
    if (data is Map) {
      final message = data['message'];
      if (message is String && message.trim().isNotEmpty) {
        return message;
      }
      final title = data['title'];
      if (title is String && title.trim().isNotEmpty) {
        return title;
      }
      final errors = data['errors'];
      if (errors is Map && errors.isNotEmpty) {
        final first = errors.values.first;
        if (first is List && first.isNotEmpty) {
          return first.first.toString();
        }
        return first.toString();
      }
    }
    return fallback;
  }

  int _statusValue(String status) {
    switch (status.toUpperCase().replaceAll('-', '_').replaceAll(' ', '_')) {
      case 'INPROGRESS':
      case 'IN_PROGRESS':
        return 1;
      case 'COMPLETED':
        return 2;
      case 'SKIPPED':
        return 3;
      case 'CANCELLED':
        return 4;
      case 'SCHEDULED':
      default:
        return 0;
    }
  }
}
