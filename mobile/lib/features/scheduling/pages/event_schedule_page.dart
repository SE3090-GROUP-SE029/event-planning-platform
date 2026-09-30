import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/theme/app_colors.dart';
import '../models/event_schedule.dart';
import '../models/timeline_activity.dart';
import '../providers/scheduling_provider.dart';

class EventSchedulePage extends ConsumerWidget {
  const EventSchedulePage({super.key, required this.eventId});

  final String eventId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final scheduleAsync = ref.watch(scheduleNotifierProvider(eventId));
    final notifier = ref.read(scheduleNotifierProvider(eventId).notifier);

    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('Event Schedule Timeline'),
        backgroundColor: AppColors.canvas,
        foregroundColor: AppColors.textPrimary,
        elevation: 0,
      ),
      body: RefreshIndicator(
        onRefresh: () => notifier.refresh(),
        child: scheduleAsync.when(
          data: (schedule) => _buildScheduleBody(
            context,
            schedule,
            notifier,
            isLoading: scheduleAsync.isLoading,
          ),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, stackTrace) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(Icons.error_outline_rounded, size: 48, color: AppColors.error),
                    const SizedBox(height: 12),
                    Text(
                      'Unable to load the schedule.',
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      error.toString(),
                      textAlign: TextAlign.center,
                      style: const TextStyle(color: AppColors.textSecondary),
                    ),
                    const SizedBox(height: 16),
                    ElevatedButton(
                      onPressed: () => notifier.refresh(),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.obsidianBlack,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(999),
                        ),
                      ),
                      child: const Text('Retry'),
                    ),
                  ],
                ),
              ),
            );
          },
        ),
      ),
    );
  }

  Widget _buildScheduleBody(
    BuildContext context,
    EventSchedule schedule,
    ScheduleNotifier notifier, {
    required bool isLoading,
  }) {
    final sortedActivities = [...schedule.activities]
      ..sort((a, b) => a.startTime.compareTo(b.startTime));
    final unresolvedConflicts = schedule.conflicts
        .where((conflict) => !conflict.isResolved)
        .toList();

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (unresolvedConflicts.isNotEmpty) ...[
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: AppColors.warningBg,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: AppColors.warning.withValues(alpha: 0.25)),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(Icons.warning_amber_rounded, color: AppColors.warning),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    unresolvedConflicts.first.description,
                    style: const TextStyle(
                      color: AppColors.textPrimary,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
        ],
        if (sortedActivities.isEmpty)
          SizedBox(
            height: 260,
            child: Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.auto_awesome,
                      size: 48,
                      color: AppColors.pastelLavenderText,
                    ),
                    const SizedBox(height: 16),
                    Text(
                      'No activities scheduled yet',
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'Let AI build a timeline from your event details and vendor availability.',
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: AppColors.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 20),
                    FilledButton.icon(
                      onPressed: isLoading
                          ? null
                          : () async {
                              try {
                                await notifier.generateWithAi(schedule.id);
                              } catch (error) {
                                if (!context.mounted) return;
                                ScaffoldMessenger.of(context).showSnackBar(
                                  SnackBar(
                                    content: Text(
                                      error.toString().replaceFirst(
                                        'Exception: ',
                                        '',
                                      ),
                                    ),
                                  ),
                                );
                              }
                            },
                      icon: isLoading
                          ? const SizedBox(
                              width: 16,
                              height: 16,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            )
                          : const Icon(Icons.auto_awesome_rounded),
                      label: const Text('Generate Schedule with AI'),
                    ),
                  ],
                ),
              ),
            ),
          )
        else
          ...sortedActivities.map(
            (activity) => _ActivityCard(
              activity: activity,
              onStatusChanged: (status) => notifier.updateActivityStatus(
                activity.id,
                status,
              ),
            ),
          ),
      ],
    );
  }
}

class _ActivityCard extends StatelessWidget {
  const _ActivityCard({
    required this.activity,
    required this.onStatusChanged,
  });

  final TimelineActivity activity;
  final void Function(String status) onStatusChanged;

  @override
  Widget build(BuildContext context) {
    final startLabel = DateFormat('h:mm a').format(activity.startTime);
    final endLabel = DateFormat('h:mm a').format(activity.endTime);

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.surfacePure,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: AppColors.borderSubtle),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '$startLabel – $endLabel',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.labelSmall?.copyWith(
                        color: AppColors.textSecondary,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      activity.title,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    if (activity.description != null && activity.description!.isNotEmpty) ...[
                      const SizedBox(height: 6),
                      Text(
                        activity.description!,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: AppColors.textSecondary,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
              ConstrainedBox(
                constraints: const BoxConstraints(
                  minWidth: 120,
                  maxWidth: 150,
                ),
                child: PopupMenuButton<String>(
                  tooltip: 'Update status',
                  onSelected: onStatusChanged,
                  itemBuilder: (context) => const [
                    PopupMenuItem(value: 'Scheduled', child: Text('Scheduled')),
                    PopupMenuItem(value: 'InProgress', child: Text('In Progress')),
                    PopupMenuItem(value: 'Completed', child: Text('Completed')),
                  ],
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
                    decoration: BoxDecoration(
                      color: _statusColor(activity.status),
                      borderRadius: BorderRadius.circular(999),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Flexible(
                          child: Text(
                            _statusLabel(activity.status),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: Theme.of(context).textTheme.labelSmall?.copyWith(
                              color: AppColors.textPrimary,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                        const SizedBox(width: 4),
                        const Icon(Icons.arrow_drop_down, size: 16),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
          if (activity.assignedVendorId != null) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              decoration: BoxDecoration(
                color: AppColors.pastelBlueLight,
                borderRadius: BorderRadius.circular(999),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: Text(
                      'Vendor assigned: ${activity.assignedVendorId}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.labelSmall?.copyWith(
                        color: AppColors.pastelBlueText,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }

  static String _statusLabel(String status) {
    switch (status) {
      case 'InProgress':
        return 'In Progress';
      case 'Completed':
        return 'Completed';
      case 'Scheduled':
      default:
        return 'Scheduled';
    }
  }

  static Color _statusColor(String status) {
    switch (status) {
      case 'InProgress':
        return AppColors.pastelYellowLight;
      case 'Completed':
        return AppColors.pastelGreenLight;
      case 'Scheduled':
      default:
        return AppColors.pastelBlueLight;
    }
  }
}
