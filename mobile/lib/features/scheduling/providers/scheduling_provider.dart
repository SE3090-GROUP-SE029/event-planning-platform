import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/scheduling_api.dart';
import '../models/event_schedule.dart';

final scheduleNotifierProvider =
    AsyncNotifierProvider.family<ScheduleNotifier, EventSchedule, String>(
  (eventId) => ScheduleNotifier(eventId),
);

class ScheduleNotifier extends AsyncNotifier<EventSchedule> {
  ScheduleNotifier(this.eventId);

  final String eventId;
  late final SchedulingApi _api = SchedulingApi();

  @override
  Future<EventSchedule> build() async {
    try {
      return await _loadScheduleWithConflicts();
    } catch (error, stackTrace) {
      _logFailure('Initial schedule load', error, stackTrace);
      Error.throwWithStackTrace(error, stackTrace);
    }
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    try {
      state = AsyncData(await _loadScheduleWithConflicts());
    } catch (error, stackTrace) {
      _logFailure('Schedule refresh', error, stackTrace);
      state = AsyncError(error, stackTrace);
    }
  }

  Future<EventSchedule> _loadScheduleWithConflicts() async {
    final schedule = await _api.getSchedule(eventId);
    final conflicts = await _api.getConflicts(schedule.id);
    return schedule.copyWith(conflicts: conflicts);
  }

  Future<void> addActivity({
    required String title,
    String? description,
    required DateTime startTime,
    required DateTime endTime,
    String? assignedVendorId,
  }) async {
    final current = state.value;
    if (current == null) return;

    await _api.addActivity(
      current.id,
      title: title,
      description: description,
      startTime: startTime,
      endTime: endTime,
      assignedVendorId: assignedVendorId,
    );
    await _reloadAfterMutation();
  }

  Future<void> updateActivity({
    required String activityId,
    required String title,
    String? description,
    required DateTime startTime,
    required DateTime endTime,
    String? assignedVendorId,
  }) async {
    await _api.updateActivity(
      activityId,
      title: title,
      description: description,
      startTime: startTime,
      endTime: endTime,
      assignedVendorId: assignedVendorId,
    );
    await _reloadAfterMutation();
  }

  Future<void> deleteActivity(String activityId) async {
    await _api.deleteActivity(activityId);
    await _reloadAfterMutation();
  }

  Future<void> updateActivityStatus(String activityId, String newStatus) async {
    await _api.updateActivityStatus(activityId, newStatus);
    await _reloadAfterMutation();
  }

  Future<void> generateWithAi(String scheduleId) async {
    try {
      final schedule = await _api.generateAiSchedule(scheduleId);
      state = AsyncData(schedule);
    } catch (error, stackTrace) {
      _logFailure('AI schedule generation state update', error, stackTrace);
      Error.throwWithStackTrace(error, stackTrace);
    }
  }

  Future<void> _reloadAfterMutation() async {
    try {
      state = AsyncData(await _loadScheduleWithConflicts());
    } catch (error, stackTrace) {
      _logFailure('Schedule reload after mutation', error, stackTrace);
      state = AsyncError(error, stackTrace);
      Error.throwWithStackTrace(error, stackTrace);
    }
  }

  void _logFailure(String operation, Object error, StackTrace stackTrace) {
    if (!kDebugMode) return;
    debugPrint(
      '$operation failed: type=${error.runtimeType}; message=$error; '
      'innerException=not available\nstackTrace=$stackTrace',
    );
  }
}
