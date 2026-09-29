import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../../events/models/event_model.dart';
import '../api/guest_management_remote_datasource.dart';
import '../models/guest_registration_model.dart';

/// Full detail view of a single planner registration.
/// Allows planner to cancel a registration and retry invitation.
class GuestRegistrationDetailPage extends StatefulWidget {
  final AuthResponseModel auth;
  final EventModel event;
  final int registrationId;

  const GuestRegistrationDetailPage({
    super.key,
    required this.auth,
    required this.event,
    required this.registrationId,
  });

  @override
  State<GuestRegistrationDetailPage> createState() =>
      _GuestRegistrationDetailPageState();
}

class _GuestRegistrationDetailPageState
    extends State<GuestRegistrationDetailPage> {
  late final GuestManagementRemoteDataSource _api;
  PlannerRegistrationModel? _registration;
  bool _loading = true;
  String? _error;
  bool _actionLoading = false;

  @override
  void initState() {
    super.initState();
    _api = GuestManagementRemoteDataSource();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final result = await _api.getRegistration(
        widget.event.id,
        widget.registrationId,
      );
      if (mounted) setState(() => _registration = result);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _cancelRegistration() async {
    final confirmed = await showDialog<bool>(
          context: context,
          builder: (_) => AlertDialog(
            title: const Text('Cancel Registration?'),
            content: const Text(
              'This will cancel the guest\'s registration and notify them if they have an invitation.',
              style: TextStyle(color: AppColors.textSecondary, height: 1.4),
            ),
            actionsPadding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: const Text('Keep',
                    style: TextStyle(color: AppColors.textSecondary)),
              ),
              FilledButton(
                style: FilledButton.styleFrom(
                  backgroundColor: AppColors.error,
                  foregroundColor: Colors.white,
                ),
                onPressed: () => Navigator.pop(context, true),
                child: const Text('Cancel Registration'),
              ),
            ],
          ),
        ) ??
        false;
    if (!confirmed) return;

    setState(() => _actionLoading = true);
    try {
      final updated = await _api.cancelRegistration(
        widget.event.id,
        widget.registrationId,
      );
      if (mounted) {
        setState(() {
          _registration = updated;
          _actionLoading = false;
        });
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Registration cancelled successfully.'),
            backgroundColor: AppColors.success,
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        setState(() => _actionLoading = false);
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
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('Guest Details'),
        actions: [
          if (_registration != null &&
              _registration!.status != RegistrationStatus.cancelled)
            IconButton(
              tooltip: 'Cancel registration',
              onPressed: _actionLoading ? null : _cancelRegistration,
              icon: Container(
                width: 38,
                height: 38,
                decoration: BoxDecoration(
                  color: AppColors.errorBg,
                  borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
                  border: Border.all(color: const Color(0x30C7434D)),
                ),
                child: const Icon(
                  Icons.person_remove_outlined,
                  size: 18,
                  color: AppColors.error,
                ),
              ),
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
                  child: _buildContent(),
                ),
    );
  }

  Widget _buildError() {
    return ListView(
      padding: const EdgeInsets.all(AppDimens.space24),
      children: [
        const SizedBox(height: 60),
        Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline_rounded,
                size: 40, color: AppColors.error),
            const SizedBox(height: 12),
            Text(
              _error!,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppColors.textSecondary),
            ),
            const SizedBox(height: 16),
            ElevatedButton(onPressed: _load, child: const Text('Retry')),
          ],
        ),
      ],
    );
  }

  Widget _buildContent() {
    final reg = _registration!;
    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppDimens.space20,
        AppDimens.space12,
        AppDimens.space20,
        AppDimens.space32,
      ),
      children: [
        // Hero card with guest name and status badges
        PastelCard(
          padding: const EdgeInsets.all(AppDimens.space20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  PastelPillBadge(
                    text: _statusLabel(reg.status),
                    style: _statusBadgeStyle(reg.status),
                  ),
                  if (reg.rsvpStatus != null &&
                      reg.rsvpStatus != RsvpStatus.notResponded) ...[
                    const SizedBox(width: 8),
                    PastelPillBadge(
                      text: 'RSVP: ${_rsvpLabel(reg.rsvpStatus!)}',
                      style: _rsvpBadgeStyle(reg.rsvpStatus!),
                    ),
                  ],
                  if (reg.isCheckedIn) ...[
                    const SizedBox(width: 8),
                    const PastelPillBadge(
                      text: '✓ Checked In',
                      style: PastelBadgeStyle.green,
                    ),
                  ],
                ],
              ),
              const SizedBox(height: AppDimens.space16),
              Text(
                reg.fullName,
                style: const TextStyle(
                  color: AppColors.textPrimary,
                  fontSize: 22,
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.5,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                reg.emailAddress,
                style: const TextStyle(
                  color: AppColors.textSecondary,
                  fontSize: 14,
                ),
              ),
            ],
          ),
        ),

        const PastelSectionHeader(title: 'Guest Information'),

        PastelCard(
          padding: const EdgeInsets.all(AppDimens.space18),
          child: Column(
            children: [
              _infoRow(
                icon: Icons.email_outlined,
                variant: PastelIconVariant.blue,
                label: 'Email',
                value: reg.emailAddress,
              ),
              if (reg.organisation != null) ...[
                _divider(),
                _infoRow(
                  icon: Icons.business_outlined,
                  variant: PastelIconVariant.lavender,
                  label: 'Organisation',
                  value: reg.organisation!,
                ),
              ],
              if (reg.phoneNumber != null) ...[
                _divider(),
                _infoRow(
                  icon: Icons.phone_outlined,
                  variant: PastelIconVariant.green,
                  label: 'Phone',
                  value: reg.phoneNumber!,
                ),
              ],
              _divider(),
              _infoRow(
                icon: Icons.schedule_outlined,
                variant: PastelIconVariant.yellow,
                label: 'Registered',
                value: _formatDate(reg.registeredAt),
              ),
              if (reg.confirmedAt != null) ...[
                _divider(),
                _infoRow(
                  icon: Icons.check_circle_outline_rounded,
                  variant: PastelIconVariant.green,
                  label: 'Confirmed',
                  value: _formatDate(reg.confirmedAt!),
                ),
              ],
              if (reg.checkedInAt != null) ...[
                _divider(),
                _infoRow(
                  icon: Icons.login_rounded,
                  variant: PastelIconVariant.olive,
                  label: 'Checked In',
                  value: _formatDate(reg.checkedInAt!),
                ),
                if (reg.checkedInMethod != null) ...[
                  _divider(),
                  _infoRow(
                    icon: Icons.qr_code_rounded,
                    variant: PastelIconVariant.olive,
                    label: 'Check-In Method',
                    value: reg.checkedInMethod!,
                  ),
                ],
              ],
            ],
          ),
        ),

        if (reg.answers.isNotEmpty) ...[
          const PastelSectionHeader(title: 'Registration Answers'),
          PastelCard(
            padding: const EdgeInsets.all(AppDimens.space18),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: reg.answers
                  .map(
                    (a) => Padding(
                      padding: const EdgeInsets.only(bottom: AppDimens.space12),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Question ${reg.answers.indexOf(a) + 1}',
                            style: const TextStyle(
                              color: AppColors.textMuted,
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                              letterSpacing: 0.5,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            a.answer,
                            style: const TextStyle(
                              color: AppColors.textPrimary,
                              fontSize: 14,
                            ),
                          ),
                        ],
                      ),
                    ),
                  )
                  .toList(),
            ),
          ),
        ],

        // Email delivery info
        if (reg.emailDeliveryStatus != null) ...[
          const PastelSectionHeader(title: 'Invitation Email'),
          PastelCard(
            padding: const EdgeInsets.all(AppDimens.space18),
            child: Column(
              children: [
                _infoRow(
                  icon: Icons.mark_email_read_outlined,
                  variant: PastelIconVariant.blue,
                  label: 'Delivery Status',
                  value: reg.emailDeliveryStatus!,
                ),
                if (reg.sentAt != null) ...[
                  _divider(),
                  _infoRow(
                    icon: Icons.send_outlined,
                    variant: PastelIconVariant.lavender,
                    label: 'Sent At',
                    value: _formatDate(reg.sentAt!),
                  ),
                ],
                _divider(),
                _infoRow(
                  icon: Icons.repeat_rounded,
                  variant: PastelIconVariant.yellow,
                  label: 'Delivery Attempts',
                  value: '${reg.deliveryAttempts}',
                ),
              ],
            ),
          ),
        ],

        // Cancel action
        if (reg.status != RegistrationStatus.cancelled &&
            reg.status != RegistrationStatus.rejected) ...[
          const SizedBox(height: AppDimens.space8),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.error,
                side: const BorderSide(color: AppColors.error, width: 1.2),
                padding: const EdgeInsets.symmetric(vertical: 14),
              ),
              onPressed: _actionLoading ? null : _cancelRegistration,
              icon: _actionLoading
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: AppColors.error,
                      ),
                    )
                  : const Icon(Icons.person_remove_outlined, size: 18),
              label: Text(
                  _actionLoading ? 'Cancelling…' : 'Cancel Registration'),
            ),
          ),
        ],
      ],
    );
  }

  Widget _infoRow({
    required IconData icon,
    required PastelIconVariant variant,
    required String label,
    required String value,
  }) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        PastelIconBadge(
          icon: icon,
          variant: variant,
          size: 40,
          iconSize: 18,
        ),
        const SizedBox(width: AppDimens.space12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: const TextStyle(
                  color: AppColors.textSecondary,
                  fontSize: 11,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 0.3,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: const TextStyle(
                  color: AppColors.textPrimary,
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _divider() => const Padding(
        padding: EdgeInsets.symmetric(vertical: AppDimens.space10),
        child: Divider(
          height: 1,
          thickness: 0.8,
          color: Color(0x0C000000),
          indent: 52,
        ),
      );

  String _formatDate(DateTime dt) {
    final local = dt.toLocal();
    return '${local.year}-${local.month.toString().padLeft(2, '0')}-'
        '${local.day.toString().padLeft(2, '0')}  '
        '${local.hour.toString().padLeft(2, '0')}:'
        '${local.minute.toString().padLeft(2, '0')}';
  }

  String _statusLabel(RegistrationStatus status) {
    switch (status) {
      case RegistrationStatus.confirmed:
        return 'ACCEPTED';
      case RegistrationStatus.pendingAi:
        return 'PENDING AI';
      case RegistrationStatus.rejected:
        return 'REJECTED';
      case RegistrationStatus.waitingList:
        return 'WAITLISTED';
      case RegistrationStatus.cancelled:
        return 'CANCELLED';
      default:
        return 'UNKNOWN';
    }
  }

  PastelBadgeStyle _statusBadgeStyle(RegistrationStatus status) {
    switch (status) {
      case RegistrationStatus.confirmed:
        return PastelBadgeStyle.green;
      case RegistrationStatus.pendingAi:
        return PastelBadgeStyle.yellow;
      case RegistrationStatus.rejected:
        return PastelBadgeStyle.pink;
      case RegistrationStatus.waitingList:
        return PastelBadgeStyle.blue;
      default:
        return PastelBadgeStyle.neutral;
    }
  }

  String _rsvpLabel(RsvpStatus status) {
    switch (status) {
      case RsvpStatus.accepted:
        return 'Accepted';
      case RsvpStatus.declined:
        return 'Declined';
      case RsvpStatus.maybe:
        return 'Maybe';
      case RsvpStatus.attended:
        return 'Attended';
      default:
        return status.name;
    }
  }

  PastelBadgeStyle _rsvpBadgeStyle(RsvpStatus status) {
    switch (status) {
      case RsvpStatus.accepted:
        return PastelBadgeStyle.green;
      case RsvpStatus.declined:
        return PastelBadgeStyle.pink;
      case RsvpStatus.maybe:
        return PastelBadgeStyle.yellow;
      default:
        return PastelBadgeStyle.neutral;
    }
  }
}
