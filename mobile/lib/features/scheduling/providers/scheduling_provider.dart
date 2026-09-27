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
    return _api.getSchedule(eventId);
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => _api.getSchedule(eventId));
  }

  Future<void> updateActivityStatus(String activityId, String newStatus) async {
    final current = state.value;
    if (current == null) return;

    final optimisticActivities = current.activities.map((activity) {
      if (activity.id == activityId) {
        return activity.copyWith(status: newStatus);
      }
      return activity;
    }).toList();

    final optimisticSchedule = EventSchedule(
      id: current.id,
      eventId: current.eventId,
      isLocked: current.isLocked,
      activities: optimisticActivities,
      conflicts: current.conflicts,
    );

    state = AsyncData(optimisticSchedule);

    try {
      final updatedActivity = await _api.updateActivityStatus(activityId, newStatus);
      final refreshedActivities = optimisticActivities.map((activity) {
        if (activity.id == updatedActivity.id) {
          return updatedActivity;
        }
        return activity;
      }).toList();

      state = AsyncData(
        EventSchedule(
          id: current.id,
          eventId: current.eventId,
          isLocked: current.isLocked,
          activities: refreshedActivities,
          conflicts: current.conflicts,
        ),
      );
    } catch (error) {
      state = AsyncValue.data(current);
    }
  }
}
