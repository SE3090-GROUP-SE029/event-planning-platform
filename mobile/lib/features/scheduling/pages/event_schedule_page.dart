import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/theme/app_colors.dart';
import '../../auth/providers/auth_providers.dart';
import '../../bookings/api/booking_remote_datasource.dart';
import '../../bookings/models/booking_model.dart';
import '../models/event_schedule.dart';
import '../models/schedule_conflict.dart';
import '../models/timeline_activity.dart';
import '../providers/scheduling_provider.dart';

class EventSchedulePage extends ConsumerStatefulWidget {
  const EventSchedulePage({super.key, required this.eventId});

  final String eventId;

  @override
  ConsumerState<EventSchedulePage> createState() => _EventSchedulePageState();
}

class _EventSchedulePageState extends ConsumerState<EventSchedulePage> {
  final _bookingApi = BookingRemoteDataSource();
  final _searchController = TextEditingController();

  String _query = '';
  String _statusFilter = 'ALL';
  String? _loadedVendorToken;
  bool _vendorsLoading = false;
  String? _vendorsError;
  List<_ScheduleVendorOption> _vendorOptions = [];
  String? _busyActivityId;
  bool _isSubmitting = false;

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final scheduleAsync = ref.watch(scheduleNotifierProvider(widget.eventId));
    final notifier = ref.read(scheduleNotifierProvider(widget.eventId).notifier);
    final session = ref.watch(currentUserProvider);
    final isPlanner = session?.roles.contains('EVENT_PLANNER') ?? false;

    _queueVendorLoad(session?.accessToken);

    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('Event Schedule Timeline'),
        backgroundColor: AppColors.canvas,
        foregroundColor: AppColors.textPrimary,
        elevation: 0,
      ),
      floatingActionButton: isPlanner
          ? scheduleAsync.maybeWhen(
              data: (schedule) => FloatingActionButton.extended(
                onPressed: _isSubmitting
                    ? null
                    : () {
                        _openActivityForm(context, schedule: schedule);
                      },
                backgroundColor: AppColors.obsidianBlack,
                foregroundColor: Colors.white,
                icon: const Icon(Icons.add_rounded),
                label: const Text('Add Activity'),
              ),
              orElse: () => null,
            )
          : null,
      body: RefreshIndicator(
        onRefresh: () async {
          await notifier.refresh();
          await _reloadVendors();
        },
        child: scheduleAsync.when(
          data: (schedule) => _buildScheduleBody(
            context,
            schedule,
            notifier,
            isPlanner: isPlanner,
            isLoading: scheduleAsync.isLoading,
          ),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, stackTrace) {
            return _ScheduleLoadError(
              error: error,
              onRetry: () => notifier.refresh(),
            );
          },
        ),
      ),
    );
  }

  void _queueVendorLoad(String? accessToken) {
    if (accessToken == null || accessToken.isEmpty) return;
    if (_loadedVendorToken == accessToken || _vendorsLoading) return;

    _loadedVendorToken = accessToken;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) _loadVendorOptions(accessToken);
    });
  }

  Future<void> _reloadVendors() async {
    if (!mounted) return;
    final accessToken = _loadedVendorToken;
    if (accessToken == null || accessToken.isEmpty) return;
    await _loadVendorOptions(accessToken);
  }

  Future<void> _loadVendorOptions(String accessToken) async {
    if (!mounted) return;
    setState(() {
      _vendorsLoading = true;
      _vendorsError = null;
    });

    try {
      final bookings = await _bookingApi.listMine(accessToken);
      final options = _vendorOptionsFrom(bookings);
      if (!mounted) return;
      setState(() {
        _vendorOptions = options;
        _vendorsLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _vendorsError = _friendlyVendorError(error);
        _vendorsLoading = false;
      });
    }
  }

  String _friendlyVendorError(Object error) {
    final text = error.toString().replaceFirst('Exception: ', '');
    if (text.contains('500') || text.contains('Internal Server Error')) {
      return 'Booked vendors could not be loaded because the bookings API returned 500.';
    }
    if (text.contains('401')) {
      return 'Please sign in again to load booked vendors.';
    }
    if (text.contains('403')) {
      return 'This account is not allowed to load planner bookings.';
    }
    return text;
  }

  List<_ScheduleVendorOption> _vendorOptionsFrom(List<BookingModel> bookings) {
    final byVendorId = <String, _ScheduleVendorOption>{};
    for (final booking in bookings) {
      if (booking.eventId != widget.eventId || booking.vendorId.isEmpty) {
        continue;
      }
      if (_isCancelled(booking.status)) continue;

      byVendorId.putIfAbsent(
        booking.vendorId,
        () => _ScheduleVendorOption(
          id: booking.vendorId,
          name: booking.vendorBusinessName,
          serviceName: booking.serviceName,
        ),
      );
    }

    final options = byVendorId.values.toList()
      ..sort((a, b) => a.name.compareTo(b.name));
    return options;
  }

  bool _isCancelled(String status) {
    final normalized = status.toUpperCase().replaceAll(' ', '_');
    return normalized == 'CANCELLED' || normalized == 'CANCELED';
  }

  Widget _buildScheduleBody(
    BuildContext context,
    EventSchedule schedule,
    ScheduleNotifier notifier, {
    required bool isPlanner,
    required bool isLoading,
  }) {
    final sortedActivities = [...schedule.activities]
      ..sort((a, b) => a.startTime.compareTo(b.startTime));
    final vendorLookup = {
      for (final vendor in _vendorOptions) vendor.id: vendor,
    };
    final filteredActivities = _filteredActivities(sortedActivities, vendorLookup);
    final activityLookup = {
      for (final activity in schedule.activities) activity.id: activity,
    };
    final unresolvedConflicts =
        schedule.conflicts.where((conflict) => !conflict.isResolved).toList();

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
      children: [
        _ScheduleToolbar(
          searchController: _searchController,
          query: _query,
          statusFilter: _statusFilter,
          onSearchChanged: (value) => setState(() => _query = value),
          onStatusChanged: (value) {
            if (value != null) setState(() => _statusFilter = value);
          },
          onGenerateAi: isPlanner && !isLoading && !_isSubmitting
              ? () {
                  _generateWithAi(context, notifier, schedule.id);
                }
              : null,
        ),
        const SizedBox(height: 12),
        _VendorSourceBanner(
          loading: _vendorsLoading,
          error: _vendorsError,
          options: _vendorOptions,
          onRetry: _loadedVendorToken == null ? null : _reloadVendors,
        ),
        const SizedBox(height: 12),
        _ConflictSection(
          conflicts: schedule.conflicts,
          activityLookup: activityLookup,
          onEditActivity: isPlanner
              ? (activity) {
                  _openActivityForm(
                    context,
                    schedule: schedule,
                    existingActivity: activity,
                  );
                }
              : null,
        ),
        if (unresolvedConflicts.isNotEmpty) const SizedBox(height: 12),
        if (sortedActivities.isEmpty)
          _EmptySchedule(
            canManage: isPlanner,
            isLoading: isLoading || _isSubmitting,
            onAdd: () {
              _openActivityForm(context, schedule: schedule);
            },
            onGenerateAi: () {
              _generateWithAi(context, notifier, schedule.id);
            },
          )
        else if (filteredActivities.isEmpty)
          const _NoMatchingActivities()
        else
          ...filteredActivities.map(
            (activity) => _ActivityCard(
              activity: activity,
              vendor: activity.assignedVendorId == null
                  ? null
                  : vendorLookup[activity.assignedVendorId],
              canManage: isPlanner,
              isBusy: _busyActivityId == activity.id || _isSubmitting,
              onEdit: () {
                _openActivityForm(
                  context,
                  schedule: schedule,
                  existingActivity: activity,
                );
              },
              onDelete: () {
                _confirmDelete(context, notifier, activity);
              },
              onStatusChanged: (status) {
                _updateStatus(context, notifier, activity, status);
              },
            ),
          ),
      ],
    );
  }

  List<TimelineActivity> _filteredActivities(
    List<TimelineActivity> activities,
    Map<String, _ScheduleVendorOption> vendorLookup,
  ) {
    final query = _query.trim().toLowerCase();
    return activities.where((activity) {
      if (_statusFilter != 'ALL' &&
          _normalizeStatus(activity.status) != _statusFilter) {
        return false;
      }

      if (query.isEmpty) return true;

      final vendor = activity.assignedVendorId == null
          ? null
          : vendorLookup[activity.assignedVendorId];
      final searchable = [
        activity.title,
        activity.description ?? '',
        _statusLabel(activity.status),
        vendor?.name ?? '',
        vendor?.serviceName ?? '',
        activity.assignedVendorId ?? '',
      ].join(' ').toLowerCase();

      return searchable.contains(query);
    }).toList();
  }

  Future<void> _openActivityForm(
    BuildContext context, {
    required EventSchedule schedule,
    TimelineActivity? existingActivity,
  }) async {
    if (_isSubmitting) return;

    final payload = await showModalBottomSheet<_ActivityFormPayload>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: AppColors.surfacePure,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => _ActivityFormSheet(
        activity: existingActivity,
        vendorOptions: _vendorOptions,
        eventDate: schedule.eventDate,
        eventStartTime: schedule.eventStartTime,
        eventEndTime: schedule.eventEndTime,
      ),
    );

    if (payload == null || !context.mounted) return;

    setState(() => _isSubmitting = true);
    try {
      final notifier = ref.read(scheduleNotifierProvider(widget.eventId).notifier);
      if (existingActivity == null) {
        await notifier.addActivity(
          title: payload.title,
          description: payload.description,
          startTime: payload.startTime,
          endTime: payload.endTime,
          assignedVendorId: payload.assignedVendorId,
        );
        _showSnack(context, 'Activity added.');
      } else {
        await notifier.updateActivity(
          activityId: existingActivity.id,
          title: payload.title,
          description: payload.description,
          startTime: payload.startTime,
          endTime: payload.endTime,
          assignedVendorId: payload.assignedVendorId,
        );
        _showSnack(context, 'Activity updated.');
      }
    } catch (error) {
      _showSnack(context, error.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  Future<void> _confirmDelete(
    BuildContext context,
    ScheduleNotifier notifier,
    TimelineActivity activity,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete activity?'),
        content: Text('Delete "${activity.title}" from this schedule?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            style: FilledButton.styleFrom(backgroundColor: AppColors.error),
            child: const Text('Delete'),
          ),
        ],
      ),
    );

    if (confirmed != true || !context.mounted) return;

    setState(() => _busyActivityId = activity.id);
    try {
      await notifier.deleteActivity(activity.id);
      _showSnack(context, 'Activity deleted.');
    } catch (error) {
      _showSnack(context, error.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _busyActivityId = null);
    }
  }

  Future<void> _updateStatus(
    BuildContext context,
    ScheduleNotifier notifier,
    TimelineActivity activity,
    String status,
  ) async {
    setState(() => _busyActivityId = activity.id);
    try {
      await notifier.updateActivityStatus(activity.id, status);
      _showSnack(context, 'Activity status updated.');
    } catch (error) {
      _showSnack(context, error.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _busyActivityId = null);
    }
  }

  Future<void> _generateWithAi(
    BuildContext context,
    ScheduleNotifier notifier,
    String scheduleId,
  ) async {
    if (_isSubmitting) return;

    setState(() => _isSubmitting = true);
    try {
      await notifier.generateWithAi(scheduleId);
      _showSnack(context, 'AI schedule generated.');
    } catch (error) {
      _showSnack(context, error.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  void _showSnack(BuildContext context, String message) {
    if (!context.mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message)),
    );
  }
}

class _ScheduleToolbar extends StatelessWidget {
  const _ScheduleToolbar({
    required this.searchController,
    required this.query,
    required this.statusFilter,
    required this.onSearchChanged,
    required this.onStatusChanged,
    required this.onGenerateAi,
  });

  final TextEditingController searchController;
  final String query;
  final String statusFilter;
  final ValueChanged<String> onSearchChanged;
  final ValueChanged<String?> onStatusChanged;
  final VoidCallback? onGenerateAi;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.surfacePure,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.borderSubtle),
      ),
      child: Column(
        children: [
          TextField(
            controller: searchController,
            onChanged: onSearchChanged,
            decoration: InputDecoration(
              prefixIcon: const Icon(Icons.search_rounded),
              suffixIcon: query.isEmpty
                  ? null
                  : IconButton(
                      tooltip: 'Clear search',
                      onPressed: () {
                        searchController.clear();
                        onSearchChanged('');
                      },
                      icon: const Icon(Icons.close_rounded),
                    ),
              hintText: 'Search activities, vendors, status',
              border: const OutlineInputBorder(),
              isDense: true,
            ),
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: DropdownButtonFormField<String>(
                  initialValue: statusFilter,
                  decoration: const InputDecoration(
                    prefixIcon: Icon(Icons.filter_alt_rounded),
                    border: OutlineInputBorder(),
                    isDense: true,
                  ),
                  items: const [
                    DropdownMenuItem(value: 'ALL', child: Text('All statuses')),
                    DropdownMenuItem(value: 'SCHEDULED', child: Text('Scheduled')),
                    DropdownMenuItem(
                      value: 'IN_PROGRESS',
                      child: Text('In Progress'),
                    ),
                    DropdownMenuItem(value: 'COMPLETED', child: Text('Completed')),
                    DropdownMenuItem(value: 'SKIPPED', child: Text('Skipped')),
                  ],
                  onChanged: onStatusChanged,
                ),
              ),
              const SizedBox(width: 10),
              IconButton.filled(
                tooltip: 'Generate schedule with AI',
                onPressed: onGenerateAi,
                icon: const Icon(Icons.auto_awesome_rounded),
                style: IconButton.styleFrom(
                  backgroundColor: AppColors.obsidianBlack,
                  foregroundColor: Colors.white,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _VendorSourceBanner extends StatelessWidget {
  const _VendorSourceBanner({
    required this.loading,
    required this.error,
    required this.options,
    required this.onRetry,
  });

  final bool loading;
  final String? error;
  final List<_ScheduleVendorOption> options;
  final Future<void> Function()? onRetry;

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return const _InfoBanner(
        icon: Icons.sync_rounded,
        text: 'Loading booked vendors...',
        color: AppColors.pastelBlueLight,
      );
    }

    if (error != null) {
      return _InfoBanner(
        icon: Icons.error_outline_rounded,
        text: 'Vendor options unavailable: $error',
        color: AppColors.errorBg,
        trailing: onRetry == null
            ? null
            : TextButton(
                onPressed: () {
                  onRetry!();
                },
                child: const Text('Retry'),
              ),
      );
    }

    if (options.isEmpty) {
      return const _InfoBanner(
        icon: Icons.storefront_outlined,
        text: 'No booked vendors are available for this event yet.',
        color: AppColors.surfaceMuted,
      );
    }

    return _InfoBanner(
      icon: Icons.storefront_rounded,
      text: '${options.length} booked vendor option(s) available.',
      color: AppColors.pastelGreenLight,
    );
  }
}

class _InfoBanner extends StatelessWidget {
  const _InfoBanner({
    required this.icon,
    required this.text,
    required this.color,
    this.trailing,
  });

  final IconData icon;
  final String text;
  final Color color;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.borderSubtle),
      ),
      child: Row(
        children: [
          Icon(icon, color: AppColors.textSecondary),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              text,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: AppColors.textSecondary,
                    fontWeight: FontWeight.w600,
                  ),
            ),
          ),
          if (trailing != null) trailing!,
        ],
      ),
    );
  }
}

class _ConflictSection extends StatelessWidget {
  const _ConflictSection({
    required this.conflicts,
    required this.activityLookup,
    required this.onEditActivity,
  });

  final List<ScheduleConflict> conflicts;
  final Map<String, TimelineActivity> activityLookup;
  final void Function(TimelineActivity activity)? onEditActivity;

  @override
  Widget build(BuildContext context) {
    if (conflicts.isEmpty) {
      return const _InfoBanner(
        icon: Icons.verified_rounded,
        text: 'No schedule conflicts reported.',
        color: AppColors.successBg,
      );
    }

    final sortedConflicts = [...conflicts]
      ..sort((a, b) => a.isResolved == b.isResolved
          ? a.conflictType.compareTo(b.conflictType)
          : a.isResolved
              ? 1
              : -1);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        ...sortedConflicts.map(
          (conflict) => _ConflictCard(
            conflict: conflict,
            activityLookup: activityLookup,
            onEditActivity: onEditActivity,
          ),
        ),
      ],
    );
  }
}

class _ConflictCard extends StatelessWidget {
  const _ConflictCard({
    required this.conflict,
    required this.activityLookup,
    required this.onEditActivity,
  });

  final ScheduleConflict conflict;
  final Map<String, TimelineActivity> activityLookup;
  final void Function(TimelineActivity activity)? onEditActivity;

  @override
  Widget build(BuildContext context) {
    final first = conflict.activityId1 == null
        ? null
        : activityLookup[conflict.activityId1];
    final second = conflict.activityId2 == null
        ? null
        : activityLookup[conflict.activityId2];
    final isResolved = conflict.isResolved;

    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: isResolved ? AppColors.surfaceMuted : AppColors.warningBg,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isResolved
              ? AppColors.borderSubtle
              : AppColors.warning.withValues(alpha: 0.25),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                isResolved
                    ? Icons.check_circle_outline_rounded
                    : Icons.warning_amber_rounded,
                color: isResolved ? AppColors.success : AppColors.warning,
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  _conflictTitle(conflict),
                  style: Theme.of(context).textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary,
                      ),
                ),
              ),
              _StatePill(
                label: isResolved ? 'Resolved' : 'Unresolved',
                color: isResolved
                    ? AppColors.pastelGreenLight
                    : AppColors.pastelYellowLight,
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            conflict.description,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: AppColors.textSecondary,
                ),
          ),
          if (first != null || second != null) ...[
            const SizedBox(height: 10),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                if (first != null)
                  _ActivityEditChip(
                    activity: first,
                    onEditActivity: onEditActivity,
                  ),
                if (second != null)
                  _ActivityEditChip(
                    activity: second,
                    onEditActivity: onEditActivity,
                  ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  String _conflictTitle(ScheduleConflict conflict) {
    switch (conflict.conflictType) {
      case 'ActivityOverlap':
        return 'Schedule overlap';
      case 'VendorDoubleBooked':
        return 'Vendor conflict';
    }

    final type = conflict.conflictType.replaceAll('_', ' ').toLowerCase();
    if (type.isEmpty) return 'Schedule conflict';
    return '${type[0].toUpperCase()}${type.substring(1)}';
  }
}

class _ActivityEditChip extends StatelessWidget {
  const _ActivityEditChip({
    required this.activity,
    required this.onEditActivity,
  });

  final TimelineActivity activity;
  final void Function(TimelineActivity activity)? onEditActivity;

  @override
  Widget build(BuildContext context) {
    return ActionChip(
      avatar: const Icon(Icons.edit_calendar_rounded, size: 18),
      label: Text(
        activity.title,
        overflow: TextOverflow.ellipsis,
      ),
      onPressed: onEditActivity == null ? null : () => onEditActivity!(activity),
    );
  }
}

class _ActivityCard extends StatelessWidget {
  const _ActivityCard({
    required this.activity,
    required this.vendor,
    required this.canManage,
    required this.isBusy,
    required this.onEdit,
    required this.onDelete,
    required this.onStatusChanged,
  });

  final TimelineActivity activity;
  final _ScheduleVendorOption? vendor;
  final bool canManage;
  final bool isBusy;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  final void Function(String status) onStatusChanged;

  @override
  Widget build(BuildContext context) {
    final startLabel = DateFormat('MMM d, h:mm a').format(activity.startTime);
    final endLabel = DateFormat('h:mm a').format(activity.endTime);
    final vendorLabel = vendor?.label ?? activity.assignedVendorId;

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.surfacePure,
        borderRadius: BorderRadius.circular(12),
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
                      '$startLabel - $endLabel',
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
                    if (activity.description != null &&
                        activity.description!.isNotEmpty) ...[
                      const SizedBox(height: 6),
                      Text(
                        activity.description!,
                        maxLines: 3,
                        overflow: TextOverflow.ellipsis,
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              color: AppColors.textSecondary,
                            ),
                      ),
                    ],
                  ],
                ),
              ),
              if (canManage) ...[
                const SizedBox(width: 8),
                PopupMenuButton<String>(
                  tooltip: 'Activity actions',
                  enabled: !isBusy,
                  onSelected: (value) {
                    if (value == 'edit') onEdit();
                    if (value == 'delete') onDelete();
                  },
                  itemBuilder: (context) => const [
                    PopupMenuItem(
                      value: 'edit',
                      child: ListTile(
                        leading: Icon(Icons.edit_rounded),
                        title: Text('Edit'),
                        contentPadding: EdgeInsets.zero,
                      ),
                    ),
                    PopupMenuItem(
                      value: 'delete',
                      child: ListTile(
                        leading: Icon(Icons.delete_outline_rounded),
                        title: Text('Delete'),
                        contentPadding: EdgeInsets.zero,
                      ),
                    ),
                  ],
                ),
              ],
            ],
          ),
          const SizedBox(height: 12),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              _StatusMenu(
                status: activity.status,
                enabled: canManage && !isBusy,
                onStatusChanged: onStatusChanged,
              ),
              if (vendorLabel != null && vendorLabel.isNotEmpty)
                _StatePill(
                  label: vendorLabel,
                  icon: Icons.storefront_rounded,
                  color: AppColors.pastelBlueLight,
                )
              else
                const _StatePill(
                  label: 'Unassigned',
                  icon: Icons.person_off_outlined,
                  color: AppColors.surfaceMuted,
                ),
              if (isBusy)
                const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
            ],
          ),
        ],
      ),
    );
  }
}

class _StatusMenu extends StatelessWidget {
  const _StatusMenu({
    required this.status,
    required this.enabled,
    required this.onStatusChanged,
  });

  final String status;
  final bool enabled;
  final void Function(String status) onStatusChanged;

  @override
  Widget build(BuildContext context) {
    return PopupMenuButton<String>(
      tooltip: 'Update status',
      enabled: enabled,
      onSelected: onStatusChanged,
      itemBuilder: (context) => const [
        PopupMenuItem(value: 'SCHEDULED', child: Text('Scheduled')),
        PopupMenuItem(value: 'IN_PROGRESS', child: Text('In Progress')),
        PopupMenuItem(value: 'COMPLETED', child: Text('Completed')),
        PopupMenuItem(value: 'SKIPPED', child: Text('Skipped')),
      ],
      child: _StatePill(
        label: _statusLabel(status),
        icon: Icons.expand_more_rounded,
        color: _statusColor(status),
      ),
    );
  }
}

class _StatePill extends StatelessWidget {
  const _StatePill({
    required this.label,
    required this.color,
    this.icon,
  });

  final String label;
  final Color color;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 16, color: AppColors.textPrimary),
            const SizedBox(width: 4),
          ],
          Flexible(
            child: Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(context).textTheme.labelSmall?.copyWith(
                    color: AppColors.textPrimary,
                    fontWeight: FontWeight.w700,
                  ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ActivityFormSheet extends StatefulWidget {
  const _ActivityFormSheet({
    required this.activity,
    required this.vendorOptions,
    required this.eventDate,
    required this.eventStartTime,
    required this.eventEndTime,
  });

  final TimelineActivity? activity;
  final List<_ScheduleVendorOption> vendorOptions;
  final DateTime? eventDate;
  final Duration? eventStartTime;
  final Duration? eventEndTime;

  @override
  State<_ActivityFormSheet> createState() => _ActivityFormSheetState();
}

class _ActivityFormSheetState extends State<_ActivityFormSheet> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _titleController;
  late final TextEditingController _descriptionController;
  late DateTime _startTime;
  late DateTime _endTime;
  String? _assignedVendorId;

  @override
  void initState() {
    super.initState();
    final activity = widget.activity;
    final eventDate = widget.eventDate ?? DateTime.now();
    final defaultStart = _combineDateAndDuration(
      eventDate,
      widget.eventStartTime ?? const Duration(hours: 9),
    );
    _titleController = TextEditingController(text: activity?.title ?? '');
    _descriptionController = TextEditingController(
      text: activity?.description ?? '',
    );
    _startTime = activity?.startTime ?? defaultStart;
    _endTime = activity?.endTime ??
        _combineDateAndDuration(
          eventDate,
          widget.eventEndTime ??
              ((widget.eventStartTime ?? const Duration(hours: 9)) +
                  const Duration(hours: 1)),
        );
    _assignedVendorId = activity?.assignedVendorId;
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).viewInsets.bottom;
    final isEditing = widget.activity != null;
    final vendorOptions = _vendorOptionsForForm;

    return Padding(
      padding: EdgeInsets.fromLTRB(16, 16, 16, bottomInset + 16),
      child: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      isEditing ? 'Edit Activity' : 'Add Activity',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            fontWeight: FontWeight.w800,
                            color: AppColors.textPrimary,
                          ),
                    ),
                  ),
                  IconButton(
                    tooltip: 'Close',
                    onPressed: () => Navigator.of(context).pop(),
                    icon: const Icon(Icons.close_rounded),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              TextFormField(
                controller: _titleController,
                decoration: const InputDecoration(
                  labelText: 'Activity title',
                  border: OutlineInputBorder(),
                ),
                textInputAction: TextInputAction.next,
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Activity title is required.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _descriptionController,
                decoration: const InputDecoration(
                  labelText: 'Description',
                  border: OutlineInputBorder(),
                ),
                minLines: 2,
                maxLines: 4,
              ),
              const SizedBox(height: 12),
              _EventDateContext(
                eventDate: widget.eventDate,
                eventStartTime: widget.eventStartTime,
                eventEndTime: widget.eventEndTime,
              ),
              const SizedBox(height: 12),
              _TimePickerTile(
                label: 'Start time',
                value: _startTime,
                onPick: () {
                  _pickTime(isStart: true);
                },
              ),
              const SizedBox(height: 10),
              _TimePickerTile(
                label: 'End time',
                value: _endTime,
                onPick: () {
                  _pickTime(isStart: false);
                },
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String?>(
                initialValue: _vendorValue,
                decoration: const InputDecoration(
                  labelText: 'Assigned vendor',
                  border: OutlineInputBorder(),
                ),
                items: [
                  const DropdownMenuItem<String?>(
                    value: null,
                    child: Text('Unassigned'),
                  ),
                  ...vendorOptions.map(
                    (vendor) => DropdownMenuItem<String?>(
                      value: vendor.id,
                      child: Text(
                        vendor.label,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ),
                ],
                onChanged: (value) => setState(() => _assignedVendorId = value),
              ),
              if (widget.vendorOptions.isEmpty) ...[
                const SizedBox(height: 8),
                Text(
                  'Vendor choices come from confirmed bookings for this event.',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: AppColors.textSecondary,
                      ),
                ),
              ],
              const SizedBox(height: 18),
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: _submit,
                  icon: Icon(isEditing ? Icons.save_rounded : Icons.add_rounded),
                  label: Text(isEditing ? 'Save Activity' : 'Add Activity'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  String? get _vendorValue {
    if (_assignedVendorId == null) return null;
    final exists = _vendorOptionsForForm.any(
      (vendor) => vendor.id == _assignedVendorId,
    );
    return exists ? _assignedVendorId : null;
  }

  List<_ScheduleVendorOption> get _vendorOptionsForForm {
    if (_assignedVendorId == null) return widget.vendorOptions;
    final exists = widget.vendorOptions.any(
      (vendor) => vendor.id == _assignedVendorId,
    );
    if (exists) return widget.vendorOptions;
    return [
      ...widget.vendorOptions,
      _ScheduleVendorOption(
        id: _assignedVendorId!,
        name: 'Currently assigned vendor',
        serviceName: _assignedVendorId!,
      ),
    ];
  }

  Future<void> _pickTime({required bool isStart}) async {
    final initial = isStart ? _startTime : _endTime;
    final time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(initial),
    );
    if (time == null || !mounted) return;

    final date = widget.eventDate ?? initial;
    final selected = DateTime(
      date.year,
      date.month,
      date.day,
      time.hour,
      time.minute,
    );

    setState(() {
      if (isStart) {
        final duration = _endTime.difference(_startTime);
        _startTime = selected;
        _endTime = selected.add(
          duration.isNegative || duration == Duration.zero
              ? const Duration(hours: 1)
              : duration,
        );
      } else {
        _endTime = selected;
      }
    });
  }

  void _submit() {
    if (!_formKey.currentState!.validate()) return;

    if (!_endTime.isAfter(_startTime)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('End time must be after start time.')),
      );
      return;
    }

    Navigator.of(context).pop(
      _ActivityFormPayload(
        title: _titleController.text.trim(),
        description: _descriptionController.text.trim().isEmpty
            ? null
            : _descriptionController.text.trim(),
        startTime: _startTime,
        endTime: _endTime,
        assignedVendorId: _assignedVendorId,
      ),
    );
  }
}

class _EventDateContext extends StatelessWidget {
  const _EventDateContext({
    required this.eventDate,
    required this.eventStartTime,
    required this.eventEndTime,
  });

  final DateTime? eventDate;
  final Duration? eventStartTime;
  final Duration? eventEndTime;

  @override
  Widget build(BuildContext context) {
    final date = eventDate == null
        ? 'Event date unavailable'
        : DateFormat('MMM d, yyyy').format(eventDate!);
    final window = eventStartTime == null || eventEndTime == null
        ? null
        : '${_formatDurationTime(context, eventStartTime!)} - ${_formatDurationTime(context, eventEndTime!)}';

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.surfaceMuted,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.borderSubtle),
      ),
      child: Text(
        window == null ? date : '$date | $window',
        style: Theme.of(context).textTheme.bodySmall?.copyWith(
              color: AppColors.textSecondary,
              fontWeight: FontWeight.w700,
            ),
      ),
    );
  }
}

class _TimePickerTile extends StatelessWidget {
  const _TimePickerTile({
    required this.label,
    required this.value,
    required this.onPick,
  });

  final String label;
  final DateTime value;
  final VoidCallback onPick;

  @override
  Widget build(BuildContext context) {
    return OutlinedButton.icon(
      onPressed: onPick,
      icon: const Icon(Icons.schedule_rounded),
      label: Align(
        alignment: Alignment.centerLeft,
        child: Text(
          '$label: ${DateFormat('h:mm a').format(value)}',
          overflow: TextOverflow.ellipsis,
        ),
      ),
      style: OutlinedButton.styleFrom(
        minimumSize: const Size.fromHeight(52),
        alignment: Alignment.centerLeft,
      ),
    );
  }
}

DateTime _combineDateAndDuration(DateTime date, Duration time) => DateTime(
      date.year,
      date.month,
      date.day,
      time.inHours % 24,
      time.inMinutes % 60,
    );

String _formatDurationTime(BuildContext context, Duration value) {
  final normalized = value.inMinutes % (24 * 60);
  return TimeOfDay(hour: normalized ~/ 60, minute: normalized % 60)
      .format(context);
}

class _EmptySchedule extends StatelessWidget {
  const _EmptySchedule({
    required this.canManage,
    required this.isLoading,
    required this.onAdd,
    required this.onGenerateAi,
  });

  final bool canManage;
  final bool isLoading;
  final VoidCallback onAdd;
  final VoidCallback onGenerateAi;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 300,
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
                'Create activities manually or generate a persisted AI timeline.',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      color: AppColors.textSecondary,
                    ),
              ),
              if (canManage) ...[
                const SizedBox(height: 18),
                Wrap(
                  spacing: 10,
                  runSpacing: 10,
                  alignment: WrapAlignment.center,
                  children: [
                    FilledButton.icon(
                      onPressed: isLoading ? null : onAdd,
                      icon: const Icon(Icons.add_rounded),
                      label: const Text('Add Activity'),
                    ),
                    OutlinedButton.icon(
                      onPressed: isLoading ? null : onGenerateAi,
                      icon: const Icon(Icons.auto_awesome_rounded),
                      label: const Text('Generate AI'),
                    ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _NoMatchingActivities extends StatelessWidget {
  const _NoMatchingActivities();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(24),
      alignment: Alignment.center,
      child: Text(
        'No activities match the current filters.',
        textAlign: TextAlign.center,
        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: AppColors.textSecondary,
            ),
      ),
    );
  }
}

class _ScheduleLoadError extends StatelessWidget {
  const _ScheduleLoadError({
    required this.error,
    required this.onRetry,
  });

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(
              Icons.error_outline_rounded,
              size: 48,
              color: AppColors.error,
            ),
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
              onPressed: onRetry,
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
  }
}

class _ScheduleVendorOption {
  const _ScheduleVendorOption({
    required this.id,
    required this.name,
    required this.serviceName,
  });

  final String id;
  final String name;
  final String serviceName;

  String get label => '$name - $serviceName';
}

class _ActivityFormPayload {
  const _ActivityFormPayload({
    required this.title,
    required this.description,
    required this.startTime,
    required this.endTime,
    required this.assignedVendorId,
  });

  final String title;
  final String? description;
  final DateTime startTime;
  final DateTime endTime;
  final String? assignedVendorId;
}

String _normalizeStatus(String status) {
  final value = status.toUpperCase().replaceAll('-', '_').replaceAll(' ', '_');
  if (value == 'INPROGRESS') return 'IN_PROGRESS';
  return value;
}

String _statusLabel(String status) {
  switch (_normalizeStatus(status)) {
    case 'IN_PROGRESS':
      return 'In Progress';
    case 'COMPLETED':
      return 'Completed';
    case 'SKIPPED':
      return 'Skipped';
    case 'CANCELLED':
    case 'CANCELED':
      return 'Cancelled';
    case 'SCHEDULED':
    default:
      return 'Scheduled';
  }
}

Color _statusColor(String status) {
  switch (_normalizeStatus(status)) {
    case 'IN_PROGRESS':
      return AppColors.pastelYellowLight;
    case 'COMPLETED':
      return AppColors.pastelGreenLight;
    case 'SKIPPED':
      return AppColors.pastelPeachLight;
    case 'CANCELLED':
    case 'CANCELED':
      return AppColors.errorBg;
    case 'SCHEDULED':
    default:
      return AppColors.pastelBlueLight;
  }
}
