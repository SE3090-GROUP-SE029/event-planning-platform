import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/event_remote_datasource.dart';
import '../models/event_model.dart';

class EventDetailsPage extends StatefulWidget {
  const EventDetailsPage({super.key});

  @override
  State<EventDetailsPage> createState() => _EventDetailsPageState();
}

class _EventDetailsPageState extends State<EventDetailsPage> {
  final _api = EventRemoteDataSource();
  EventModel? _event;
  AuthResponseModel? _auth;
  String? _error;
  bool _loading = true;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_auth == null) {
      final args = ModalRoute.of(context)?.settings.arguments as Map?;
      _auth = args?['auth'] as AuthResponseModel?;
      _event = args?['event'] as EventModel?;
      _load();
    }
  }

  Future<void> _load() async {
    if (_auth == null || _event == null) {
      setState(() {
        _loading = false;
        _error = 'Event not found.';
      });
      return;
    }
    try {
      final result = await _api.get(_auth!.accessToken, _event!.id);
      if (mounted) {
        setState(() {
          _event = result;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString();
          _loading = false;
        });
      }
    }
  }

  Future<void> _delete() async {
    final confirmed = await showDialog<bool>(
          context: context,
          builder: (_) => AlertDialog(
            title: const Text('Delete event?'),
            content: const Text(
              'This action cannot be undone. Are you sure you want to permanently delete this event plan?',
              style: TextStyle(color: AppColors.textSecondary, height: 1.4),
            ),
            actionsPadding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: const Text('Cancel',
                    style: TextStyle(color: AppColors.textSecondary)),
              ),
              FilledButton(
                style: FilledButton.styleFrom(
                  backgroundColor: AppColors.error,
                  foregroundColor: Colors.white,
                ),
                onPressed: () => Navigator.pop(context, true),
                child: const Text('Delete'),
              ),
            ],
          ),
        ) ??
        false;
    if (!confirmed || _auth == null || _event == null) return;
    try {
      await _api.delete(_auth!.accessToken, _event!.id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Event deleted successfully.'),
          backgroundColor: AppColors.success,
        ),
      );
      Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.toString()),
            backgroundColor: AppColors.error,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final event = _event;
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('Event details'),
        actions: [
          if (event != null) ...[
            IconButton(
              tooltip: 'Edit event',
              onPressed: () => Navigator.pushNamed(
                context,
                '/events/edit',
                arguments: {'auth': _auth, 'event': event},
              ),
              icon: Container(
                width: 38,
                height: 38,
                decoration: BoxDecoration(
                  color: AppColors.surfacePure,
                  borderRadius: BorderRadius.circular(AppDimens.radiusSmall),
                  border: Border.all(color: AppColors.borderSubtle),
                ),
                child: const Icon(
                  Icons.edit_outlined,
                  size: 18,
                  color: AppColors.textPrimary,
                ),
              ),
            ),
            IconButton(
              tooltip: 'Delete event',
              onPressed: _delete,
              icon: Container(
                width: 38,
                height: 38,
                decoration: BoxDecoration(
                  color: AppColors.errorBg,
                  borderRadius: BorderRadius.circular(AppDimens.radiusSmall),
                  border: Border.all(color: const Color(0x30C7434D)),
                ),
                child: const Icon(
                  Icons.delete_outline_rounded,
                  size: 18,
                  color: AppColors.error,
                ),
              ),
            ),
            const SizedBox(width: AppDimens.space12),
          ],
        ],
      ),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(
                valueColor:
                    AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
              ),
            )
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(AppDimens.space24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          _error!,
                          textAlign: TextAlign.center,
                          style: const TextStyle(color: AppColors.error),
                        ),
                        const SizedBox(height: AppDimens.space16),
                        ElevatedButton(
                          onPressed: _load,
                          child: const Text('Retry'),
                        ),
                      ],
                    ),
                  ),
                )
              : RefreshIndicator(
                  color: AppColors.obsidianBlack,
                  backgroundColor: AppColors.surfacePure,
                  onRefresh: _load,
                  child: ListView(
                    padding: const EdgeInsets.fromLTRB(
                      AppDimens.space20,
                      AppDimens.space12,
                      AppDimens.space20,
                      AppDimens.space32,
                    ),
                    children: [
                      // Hero Card
                      PastelCard(
                        padding: const EdgeInsets.all(AppDimens.space24),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                PastelPillBadge(
                                  text: event!.eventType.name.toUpperCase(),
                                  style: _getTypeBadgeStyle(event.eventType),
                                ),
                                const Spacer(),
                                PastelPillBadge(
                                  text: event.status.name.toUpperCase(),
                                  style: _getStatusBadgeStyle(event.status),
                                ),
                              ],
                            ),
                            const SizedBox(height: AppDimens.space18),
                            Text(
                              event.preferredVenue.isEmpty
                                  ? 'Untitled Event'
                                  : event.preferredVenue,
                              style: const TextStyle(
                                color: AppColors.textPrimary,
                                fontSize: 24,
                                fontWeight: FontWeight.w800,
                                letterSpacing: -0.6,
                              ),
                            ),
                          ],
                        ),
                      ),

                      const PastelSectionHeader(title: 'Overview'),

                      // Information Grid Card
                      PastelCard(
                        padding: const EdgeInsets.all(AppDimens.space18),
                        child: Column(
                          children: [
                            _buildDetailRow(
                              icon: Icons.calendar_today_outlined,
                              variant: PastelIconVariant.yellow,
                              label: 'Event date',
                              value: event.preferredDate
                                  .toLocal()
                                  .toString()
                                  .split(' ')
                                  .first,
                            ),
                            const Divider(
                              height: 24,
                              thickness: 0.8,
                              color: Color(0x0C000000),
                              indent: 56,
                            ),
                            _buildDetailRow(
                              icon: Icons.people_outline_rounded,
                              variant: PastelIconVariant.pink,
                              label: 'Guest count',
                              value: '${event.guestCount} guests',
                            ),
                            const Divider(
                              height: 24,
                              thickness: 0.8,
                              color: Color(0x0C000000),
                              indent: 56,
                            ),
                            _buildDetailRow(
                              icon: Icons.attach_money_rounded,
                              variant: PastelIconVariant.green,
                              label: 'Budget',
                              value: '\$${event.budget.toStringAsFixed(2)}',
                            ),
                            const Divider(
                              height: 24,
                              thickness: 0.8,
                              color: Color(0x0C000000),
                              indent: 56,
                            ),
                            _buildDetailRow(
                              icon: Icons.history_rounded,
                              variant: PastelIconVariant.blue,
                              label: 'Created',
                              value: event.createdAt
                                  .toLocal()
                                  .toString()
                                  .split(' ')
                                  .first,
                            ),
                          ],
                        ),
                      ),

                      const PastelSectionHeader(title: 'Requirements & Notes'),

                      // Requirements Note Card
                      PastelCard(
                        padding: const EdgeInsets.all(AppDimens.space20),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const PastelIconBadge(
                              icon: Icons.notes_rounded,
                              variant: PastelIconVariant.lavender,
                              size: 40,
                              iconSize: 20,
                            ),
                            const SizedBox(width: AppDimens.space14),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'Special instructions',
                                    style: TextStyle(
                                      color: AppColors.textPrimary,
                                      fontSize: 14,
                                      fontWeight: FontWeight.w700,
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    event.requirements?.isNotEmpty == true
                                        ? event.requirements!
                                        : 'No special requirements noted for this event.',
                                    style: const TextStyle(
                                      color: AppColors.textSecondary,
                                      fontSize: 14,
                                      height: 1.4,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
    );
  }

  Widget _buildDetailRow({
    required IconData icon,
    required PastelIconVariant variant,
    required String label,
    required String value,
  }) {
    return Row(
      children: [
        PastelIconBadge(
          icon: icon,
          variant: variant,
          size: 42,
          iconSize: 20,
        ),
        const SizedBox(width: AppDimens.space14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: const TextStyle(
                  color: AppColors.textSecondary,
                  fontSize: 12,
                  fontWeight: FontWeight.w500,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: const TextStyle(
                  color: AppColors.textPrimary,
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  PastelBadgeStyle _getTypeBadgeStyle(EventType type) {
    switch (type) {
      case EventType.wedding:
        return PastelBadgeStyle.pink;
      case EventType.birthday:
        return PastelBadgeStyle.yellow;
      case EventType.corporate:
        return PastelBadgeStyle.blue;
      case EventType.anniversary:
        return PastelBadgeStyle.lavender;
      case EventType.other:
        return PastelBadgeStyle.neutral;
    }
  }

  PastelBadgeStyle _getStatusBadgeStyle(EventStatus status) {
    switch (status) {
      case EventStatus.confirmed:
        return PastelBadgeStyle.green;
      case EventStatus.draft:
        return PastelBadgeStyle.yellow;
      case EventStatus.planning:
      case EventStatus.inPlanning:
        return PastelBadgeStyle.blue;
      case EventStatus.completed:
        return PastelBadgeStyle.oliveRibbon;
      case EventStatus.cancelled:
        return PastelBadgeStyle.neutral;
    }
  }
}
