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
    return _loadScheduleWithConflicts();
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(_loadScheduleWithConflicts);
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
    state = const AsyncLoading();
    try {
      final schedule = await _api.generateAiSchedule(scheduleId);
      final conflicts = await _api.getConflicts(schedule.id);
      state = AsyncData(schedule.copyWith(conflicts: conflicts));
    } catch (error, stackTrace) {
      state = AsyncError(error, stackTrace);
      Error.throwWithStackTrace(error, stackTrace);
    }
  }

  Future<void> _reloadAfterMutation() async {
    try {
      state = AsyncData(await _loadScheduleWithConflicts());
    } catch (error, stackTrace) {
      state = AsyncError(error, stackTrace);
      Error.throwWithStackTrace(error, stackTrace);
    }
  }
}
