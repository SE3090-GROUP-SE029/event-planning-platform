import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../../events/models/event_model.dart';
import '../api/guest_management_remote_datasource.dart';
import '../models/guest_registration_model.dart';

/// Event guest analytics dashboard for the planner.
///
/// Analytics are computed client-side from a full page listing of registrations
/// (using a large page size) since the backend analytics endpoint is admin-only.
/// This approach is intentional and does not add admin-scope access.
class GuestAnalyticsPage extends StatefulWidget {
  final AuthResponseModel auth;
  final EventModel event;

  const GuestAnalyticsPage({
    super.key,
    required this.auth,
    required this.event,
  });

  @override
  State<GuestAnalyticsPage> createState() => _GuestAnalyticsPageState();
}

class _GuestAnalyticsPageState extends State<GuestAnalyticsPage> {
  final _api = GuestManagementRemoteDataSource();
  List<PlannerRegistrationModel>? _guests;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      // Fetch a large page to compute analytics (backend max pageSize = 50,
      // so we chain pages — but for analytics overview a single large call is fine)
      final all = <PlannerRegistrationModel>[];
      int page = 1;
      const pageSize = 50;
      int total = 0;
      do {
        final result = await _api.listRegistrations(
          widget.event.id,
          page: page,
          pageSize: pageSize,
        );
        all.addAll(result.items);
        total = result.total;
        page++;
      } while (all.length < total && all.length < 500);

      if (mounted) setState(() => _guests = all);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('Guest Analytics'),
        actions: [
          IconButton(
            tooltip: 'Refresh',
            icon: const Icon(Icons.refresh_rounded, size: 22),
            onPressed: _load,
          ),
          const SizedBox(width: AppDimens.space8),
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
              ? _buildError()
              : RefreshIndicator(
                  color: AppColors.obsidianBlack,
                  backgroundColor: AppColors.surfacePure,
                  onRefresh: _load,
                  child: _buildDashboard(_guests!),
                ),
    );
  }

  Widget _buildError() {
    return ListView(
      padding: const EdgeInsets.all(AppDimens.space24),
      children: [
        const SizedBox(height: 60),
        Center(
          child: Column(
            children: [
              const Icon(Icons.bar_chart_outlined,
                  size: 40, color: AppColors.error),
              const SizedBox(height: 16),
              Text(
                _error!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textSecondary),
              ),
              const SizedBox(height: 20),
              ElevatedButton.icon(
                onPressed: _load,
                icon: const Icon(Icons.refresh_rounded, size: 16),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildDashboard(List<PlannerRegistrationModel> guests) {
    final analytics = _compute(guests);

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppDimens.space18,
        AppDimens.space12,
        AppDimens.space18,
        AppDimens.space32,
      ),
      children: [
        // Event context
        PastelCard(
          padding: const EdgeInsets.all(AppDimens.space16),
          child: Row(
            children: [
              const PastelIconBadge(
                icon: Icons.event_note_rounded,
                variant: PastelIconVariant.lavender,
                size: 44,
                iconSize: 22,
              ),
              const SizedBox(width: AppDimens.space12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      widget.event.eventName.isNotEmpty
                          ? widget.event.eventName
                          : 'Event',
                      style: const TextStyle(
                        color: AppColors.textPrimary,
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.3,
                      ),
                    ),
                    Text(
                      'Analytics · ${guests.length} total guests',
                      style: const TextStyle(
                        color: AppColors.textSecondary,
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: AppDimens.space8),

        // Top metrics grid
        const PastelSectionHeader(title: 'Overview'),
        Row(
          children: [
            Expanded(
              child: _MetricCard(
                label: 'Total',
                value: '${analytics.total}',
                icon: Icons.people_outline_rounded,
                variant: PastelIconVariant.lavender,
              ),
            ),
            const SizedBox(width: AppDimens.space10),
            Expanded(
              child: _MetricCard(
                label: 'Confirmed',
                value: '${analytics.confirmed}',
                icon: Icons.check_circle_outline_rounded,
                variant: PastelIconVariant.green,
                color: AppColors.pastelGreenLight,
              ),
            ),
          ],
        ),
        const SizedBox(height: AppDimens.space10),
        Row(
          children: [
            Expanded(
              child: _MetricCard(
                label: 'Pending review',
                value: '${analytics.pendingReview}',
                icon: Icons.psychology_outlined,
                variant: PastelIconVariant.yellow,
                color: AppColors.pastelYellowLight,
              ),
            ),
            const SizedBox(width: AppDimens.space10),
            Expanded(
              child: _MetricCard(
                label: 'Accepted',
                value: '${analytics.accepted}',
                icon: Icons.task_alt_rounded,
                variant: PastelIconVariant.blue,
                color: AppColors.pastelBlueLight,
              ),
            ),
          ],
        ),
        const SizedBox(height: AppDimens.space10),
        Row(
          children: [
            Expanded(
              child: _MetricCard(
                label: 'Waitlisted',
                value: '${analytics.waitlisted}',
                icon: Icons.pending_outlined,
                variant: PastelIconVariant.blue,
                color: AppColors.pastelBlueLight,
              ),
            ),
            const SizedBox(width: AppDimens.space10),
            Expanded(
              child: _MetricCard(
                label: 'Rejected',
                value: '${analytics.rejected}',
                icon: Icons.cancel_outlined,
                variant: PastelIconVariant.pink,
                color: AppColors.errorBg,
              ),
            ),
          ],
        ),
        const SizedBox(height: AppDimens.space10),
        Row(
          children: [
            Expanded(
              child: _MetricCard(
                label: 'Cancelled',
                value: '${analytics.cancelled}',
                icon: Icons.block_rounded,
                variant: PastelIconVariant.olive,
                color: AppColors.surfaceMuted,
              ),
            ),
            const SizedBox(width: AppDimens.space10),
            const Expanded(child: SizedBox()),
          ],
        ),

        const SizedBox(height: AppDimens.space8),

        // RSVP breakdown
        const PastelSectionHeader(title: 'RSVP Breakdown'),
        PastelCard(
          padding: const EdgeInsets.all(AppDimens.space18),
          child: Column(
            children: [
              _rsvpBar(
                label: 'Accepted',
                value: analytics.rsvpAccepted,
                total: analytics.confirmed,
                color: AppColors.pastelGreen,
              ),
              const SizedBox(height: AppDimens.space12),
              _rsvpBar(
                label: 'Declined',
                value: analytics.rsvpDeclined,
                total: analytics.confirmed,
                color: AppColors.error,
              ),
              const SizedBox(height: AppDimens.space12),
              _rsvpBar(
                label: 'Maybe',
                value: analytics.rsvpMaybe,
                total: analytics.confirmed,
                color: AppColors.pastelYellow,
              ),
              const SizedBox(height: AppDimens.space12),
              _rsvpBar(
                label: 'Not Responded',
                value: analytics.rsvpPending,
                total: analytics.confirmed,
                color: AppColors.borderMuted,
              ),
            ],
          ),
        ),

        const SizedBox(height: AppDimens.space8),

        // Check-In breakdown
        const PastelSectionHeader(title: 'Check-In'),
        Row(
          children: [
            Expanded(
              child: _MetricCard(
                label: 'Checked In',
                value: '${analytics.checkedIn}',
                icon: Icons.login_rounded,
                variant: PastelIconVariant.green,
                color: AppColors.pastelGreenLight,
              ),
            ),
            const SizedBox(width: AppDimens.space10),
            Expanded(
              child: _MetricCard(
                label: 'Not Checked In',
                value: '${analytics.notCheckedIn}',
                icon: Icons.login_outlined,
                variant: PastelIconVariant.yellow,
                color: AppColors.pastelYellowLight,
                subtitle: analytics.confirmed > 0
                    ? '${((analytics.notCheckedIn / analytics.confirmed) * 100).round()}% remaining'
                    : null,
              ),
            ),
          ],
        ),
        if (analytics.confirmed > 0) ...[
          const SizedBox(height: AppDimens.space10),
          PastelCard(
            padding: const EdgeInsets.all(AppDimens.space16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Check-In Rate',
                  style: TextStyle(
                    color: AppColors.textSecondary,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 8),
                ClipRRect(
                  borderRadius: BorderRadius.circular(4),
                  child: LinearProgressIndicator(
                    value: analytics.checkedIn / analytics.confirmed,
                    backgroundColor: AppColors.surfaceMuted,
                    valueColor: const AlwaysStoppedAnimation<Color>(
                        AppColors.pastelGreen),
                    minHeight: 10,
                  ),
                ),
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      '${((analytics.checkedIn / analytics.confirmed) * 100).round()}% attendance rate',
                      style: const TextStyle(
                        color: AppColors.textPrimary,
                        fontSize: 13,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    Text(
                      '${analytics.checkedIn} / ${analytics.confirmed}',
                      style: const TextStyle(
                        color: AppColors.textSecondary,
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],

        const SizedBox(height: AppDimens.space12),
        const Text(
          'Analytics are computed from the current guest list. Refresh to get the latest data.',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: AppColors.textMuted,
            fontSize: 11,
            height: 1.4,
          ),
        ),
      ],
    );
  }

  Widget _rsvpBar({
    required String label,
    required int value,
    required int total,
    required Color color,
  }) {
    final fraction = total > 0 ? value / total : 0.0;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              label,
              style: const TextStyle(
                color: AppColors.textPrimary,
                fontSize: 13,
                fontWeight: FontWeight.w600,
              ),
            ),
            Text(
              '$value',
              style: const TextStyle(
                color: AppColors.textPrimary,
                fontSize: 13,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
        const SizedBox(height: 6),
        ClipRRect(
          borderRadius: BorderRadius.circular(4),
          child: LinearProgressIndicator(
            value: fraction.clamp(0.0, 1.0),
            backgroundColor: AppColors.surfaceMuted,
            valueColor: AlwaysStoppedAnimation<Color>(color),
            minHeight: 8,
          ),
        ),
      ],
    );
  }

  _AnalyticsData _compute(List<PlannerRegistrationModel> guests) {
    int confirmed = 0,
        pendingReview = 0,
        accepted = 0,
        waitlisted = 0,
        rejected = 0,
        cancelled = 0;
    int rsvpAccepted = 0, rsvpDeclined = 0, rsvpMaybe = 0, rsvpPending = 0;
    int checkedIn = 0, notCheckedIn = 0;

    for (final g in guests) {
      switch (g.status) {
        case RegistrationStatus.confirmed:
          confirmed++;
          // RSVP
          switch (g.rsvpStatus) {
            case RsvpStatus.accepted:
              rsvpAccepted++;
              break;
            case RsvpStatus.declined:
              rsvpDeclined++;
              break;
            case RsvpStatus.maybe:
              rsvpMaybe++;
              break;
            default:
              rsvpPending++;
          }
          // Check-in
          if (g.isCheckedIn) {
            checkedIn++;
          } else {
            notCheckedIn++;
          }
          break;
        case RegistrationStatus.pendingReview:
          pendingReview++;
          break;
        case RegistrationStatus.accepted:
          accepted++;
          break;
        case RegistrationStatus.waitlisted:
          waitlisted++;
          break;
        case RegistrationStatus.rejected:
          rejected++;
          break;
        case RegistrationStatus.cancelled:
          cancelled++;
          break;
        default:
          break;
      }
    }

    return _AnalyticsData(
      total: guests.length,
      confirmed: confirmed,
      pendingReview: pendingReview,
      accepted: accepted,
      waitlisted: waitlisted,
      rejected: rejected,
      cancelled: cancelled,
      rsvpAccepted: rsvpAccepted,
      rsvpDeclined: rsvpDeclined,
      rsvpMaybe: rsvpMaybe,
      rsvpPending: rsvpPending,
      checkedIn: checkedIn,
      notCheckedIn: notCheckedIn,
    );
  }
}

class _AnalyticsData {
  final int total;
  final int confirmed;
  final int pendingReview;
  final int accepted;
  final int waitlisted;
  final int rejected;
  final int cancelled;
  final int rsvpAccepted;
  final int rsvpDeclined;
  final int rsvpMaybe;
  final int rsvpPending;
  final int checkedIn;
  final int notCheckedIn;

  const _AnalyticsData({
    required this.total,
    required this.confirmed,
    required this.pendingReview,
    required this.accepted,
    required this.waitlisted,
    required this.rejected,
    required this.cancelled,
    required this.rsvpAccepted,
    required this.rsvpDeclined,
    required this.rsvpMaybe,
    required this.rsvpPending,
    required this.checkedIn,
    required this.notCheckedIn,
  });
}

class _MetricCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final PastelIconVariant variant;
  final Color? color;
  final String? subtitle;

  const _MetricCard({
    required this.label,
    required this.value,
    required this.icon,
    required this.variant,
    this.color,
    this.subtitle,
  });

  @override
  Widget build(BuildContext context) {
    return PastelCard(
      backgroundColor: color,
      padding: const EdgeInsets.all(AppDimens.space16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          PastelIconBadge(
            icon: icon,
            variant: variant,
            size: 38,
            iconSize: 18,
          ),
          const SizedBox(height: AppDimens.space10),
          Text(
            value,
            style: const TextStyle(
              color: AppColors.textPrimary,
              fontSize: 28,
              fontWeight: FontWeight.w800,
              letterSpacing: -0.8,
            ),
          ),
          Text(
            label,
            style: const TextStyle(
              color: AppColors.textSecondary,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
          if (subtitle != null) ...[
            const SizedBox(height: 2),
            Text(
              subtitle!,
              style: const TextStyle(
                color: AppColors.textMuted,
                fontSize: 11,
              ),
            ),
          ],
        ],
      ),
    );
  }
}
