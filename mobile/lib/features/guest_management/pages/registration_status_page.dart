import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../core/theme/app_theme.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../models/guest_registration_model.dart';
import '../api/guest_management_remote_datasource.dart';
import 'guest_rsvp_page.dart';

/// Shows the result of a public guest registration submission.
/// Also acts as the status check page when returning from deep-link.
///
/// Displays registration reference, status, QR code (if confirmed),
/// and allows navigation to the RSVP page.
class RegistrationStatusPage extends StatefulWidget {
  final PublicRegistrationModel registration;
  final String eventName;
  final GuestManagementRemoteDataSource? api;

  const RegistrationStatusPage({
    super.key,
    required this.registration,
    required this.eventName,
    this.api,
  });

  @override
  State<RegistrationStatusPage> createState() => _RegistrationStatusPageState();
}

class _RegistrationStatusPageState extends State<RegistrationStatusPage> {
  late final GuestManagementRemoteDataSource _api;
  late PublicRegistrationModel _registration;
  bool _refreshing = false;
  String? _refreshError;

  @override
  void initState() {
    super.initState();
    _api = widget.api ?? GuestManagementRemoteDataSource();
    _registration = widget.registration;
  }

  Future<void> _refreshStatus() async {
    final secret = _registration.statusSecret;
    if (secret == null) {
      setState(() =>
          _refreshError = 'A registration secret is required to check status.');
      return;
    }

    setState(() {
      _refreshing = true;
      _refreshError = null;
    });
    try {
      final updated = await _api.getRegistrationStatus(
        _registration.publicReference,
        secret,
      );
      if (!mounted) return;
      setState(() => _registration = _retainStatusSecret(updated, secret));
    } catch (error) {
      if (!mounted) return;
      setState(() => _refreshError = error.toString());
    } finally {
      if (mounted) setState(() => _refreshing = false);
    }
  }

  PublicRegistrationModel _retainStatusSecret(
    PublicRegistrationModel registration,
    String? secret,
  ) =>
      PublicRegistrationModel(
        publicReference: registration.publicReference,
        status: registration.status,
        statusSecret: secret,
        invitationToken: registration.invitationToken,
        qrPngBase64: registration.qrPngBase64,
        rsvpStatus: registration.rsvpStatus,
        emailDeliveryStatus: registration.emailDeliveryStatus,
        registeredAt: registration.registeredAt,
        confirmedAt: registration.confirmedAt,
        cancelledAt: registration.cancelledAt,
      );

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('Registration Status'),
        automaticallyImplyLeading: true,
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppDimens.space20,
          AppDimens.space16,
          AppDimens.space20,
          AppDimens.space32,
        ),
        children: [
          // Status hero card
          _buildStatusCard(),
          if (_registration.statusSecret != null) ...[
            const SizedBox(height: AppDimens.space8),
            OutlinedButton.icon(
              onPressed: _refreshing ? null : _refreshStatus,
              icon: _refreshing
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.refresh_rounded),
              label: Text(_refreshing ? 'Refreshing status' : 'Refresh status'),
            ),
            if (_refreshError != null)
              Padding(
                padding: const EdgeInsets.only(top: AppDimens.space8),
                child: Text(
                  _refreshError!,
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: AppColors.error),
                ),
              ),
          ],
          const SizedBox(height: AppDimens.space16),

          // QR code card (only when CONFIRMED and QR PNG available)
          if (_registration.status == RegistrationStatus.confirmed &&
              _registration.qrPngBase64 != null)
            _buildQrCard(),

          // RSVP card (if CONFIRMED and has invitation)
          if (_registration.status == RegistrationStatus.confirmed &&
              _registration.invitationToken != null) ...[
            const SizedBox(height: AppDimens.space16),
            _buildRsvpCard(),
          ],

          // Reference copy card
          const SizedBox(height: AppDimens.space16),
          _buildReferenceCard(),

          // Status explanation
          const SizedBox(height: AppDimens.space16),
          _buildStatusExplanation(),
        ],
      ),
    );
  }

  Widget _buildStatusCard() {
    final statusInfo = _statusInfo(_registration.status);
    return PastelCard(
      backgroundColor: statusInfo.bgColor,
      padding: const EdgeInsets.all(AppDimens.space24),
      child: Column(
        children: [
          Container(
            width: 72,
            height: 72,
            decoration: BoxDecoration(
              color: statusInfo.iconBg,
              shape: BoxShape.circle,
            ),
            child: Icon(
              statusInfo.icon,
              color: statusInfo.iconColor,
              size: 38,
            ),
          ),
          const SizedBox(height: AppDimens.space16),
          Text(
            statusInfo.title,
            textAlign: TextAlign.center,
            style: TextStyle(
              color: statusInfo.textColor,
              fontSize: 22,
              fontWeight: FontWeight.w800,
              letterSpacing: -0.4,
            ),
          ),
          const SizedBox(height: AppDimens.space8),
          Text(
            statusInfo.subtitle,
            textAlign: TextAlign.center,
            style: TextStyle(
              color: statusInfo.textColor.withValues(alpha: 0.7),
              fontSize: 14,
              height: 1.4,
            ),
          ),
          const SizedBox(height: AppDimens.space16),
          PastelPillBadge(
            text: _statusLabel(_registration.status),
            style: _statusBadgeStyle(_registration.status),
          ),
        ],
      ),
    );
  }

  Widget _buildQrCard() {
    final qrBytes = base64Decode(_registration.qrPngBase64!);
    return PastelCard(
      padding: const EdgeInsets.all(AppDimens.space20),
      child: Column(
        children: [
          const Text(
            'Your Event QR Code',
            style: TextStyle(
              color: AppColors.textPrimary,
              fontSize: 16,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: AppDimens.space8),
          const Text(
            'Present this at the event entrance for check-in.',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: AppColors.textSecondary,
              fontSize: 13,
            ),
          ),
          const SizedBox(height: AppDimens.space20),
          Container(
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(AppDimens.radiusCard),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.08),
                  blurRadius: 12,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            padding: const EdgeInsets.all(16),
            child: Image.memory(
              qrBytes,
              width: 200,
              height: 200,
              fit: BoxFit.contain,
              filterQuality: FilterQuality.none,
            ),
          ),
          const SizedBox(height: AppDimens.space12),
          const Text(
            'Screenshot this QR code to access it offline.',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: AppColors.textMuted,
              fontSize: 11,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildRsvpCard() {
    final rsvp = _registration.rsvpStatus;
    final hasResponded = rsvp != null && rsvp != RsvpStatus.notResponded;
    return PastelCard(
      backgroundColor:
          hasResponded ? AppColors.pastelGreenLight : AppColors.pastelBlueLight,
      padding: const EdgeInsets.all(AppDimens.space18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              PastelIconBadge(
                icon: hasResponded
                    ? Icons.how_to_vote_outlined
                    : Icons.mail_outline_rounded,
                variant: hasResponded
                    ? PastelIconVariant.green
                    : PastelIconVariant.blue,
                size: 40,
                iconSize: 20,
              ),
              const SizedBox(width: AppDimens.space12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      hasResponded ? 'RSVP Submitted' : 'RSVP Required',
                      style: TextStyle(
                        color: hasResponded
                            ? AppColors.pastelGreenText
                            : AppColors.pastelBlueText,
                        fontSize: 15,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    Text(
                      hasResponded
                          ? 'You responded: ${_rsvpLabel(rsvp)}'
                          : 'Please confirm your attendance.',
                      style: TextStyle(
                        color: hasResponded
                            ? AppColors.pastelGreenText.withValues(alpha: 0.8)
                            : AppColors.pastelBlueText.withValues(alpha: 0.8),
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          if (!hasResponded) ...[
            const SizedBox(height: AppDimens.space14),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                style: AppButtonStyles.primary(),
                onPressed: () async {
                  final updated = await Navigator.push<PublicRegistrationModel>(
                    context,
                    MaterialPageRoute<PublicRegistrationModel>(
                      builder: (_) => GuestRsvpPage(
                        registration: _registration,
                        eventName: widget.eventName,
                        api: _api,
                      ),
                    ),
                  );
                  if (updated != null && mounted) {
                    setState(() => _registration = _retainStatusSecret(
                        updated, _registration.statusSecret));
                  }
                },
                icon: const Icon(Icons.how_to_vote_rounded, size: 16),
                label: const Text('Submit RSVP'),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildReferenceCard() {
    return PastelCard(
      padding: const EdgeInsets.all(AppDimens.space16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Registration Reference',
            style: TextStyle(
              color: AppColors.textSecondary,
              fontSize: 12,
              fontWeight: FontWeight.w600,
              letterSpacing: 0.3,
            ),
          ),
          const SizedBox(height: 6),
          Row(
            children: [
              Expanded(
                child: Text(
                  _registration.publicReference,
                  style: const TextStyle(
                    color: AppColors.textPrimary,
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    fontFamily: 'monospace',
                    letterSpacing: 0.5,
                  ),
                ),
              ),
              IconButton(
                tooltip: 'Copy reference',
                icon: const Icon(Icons.copy_rounded,
                    size: 18, color: AppColors.textSecondary),
                onPressed: () {
                  Clipboard.setData(
                    ClipboardData(text: _registration.publicReference),
                  );
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Reference copied to clipboard'),
                      duration: Duration(seconds: 2),
                    ),
                  );
                },
              ),
            ],
          ),
          if (_registration.registeredAt != DateTime(2000)) ...[
            const SizedBox(height: 4),
            Text(
              'Registered on ${_formatDate(_registration.registeredAt)}',
              style: const TextStyle(
                color: AppColors.textMuted,
                fontSize: 12,
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildStatusExplanation() {
    final explanations = {
      RegistrationStatus.pendingReview:
          'Your registration is being reviewed by the event team. Check back later for an update.',
      RegistrationStatus.accepted:
          'Your registration passed review and is waiting for seat allocation. We will notify you when its seat is confirmed.',
      RegistrationStatus.confirmed:
          'Congratulations! You have been confirmed for this event. You should receive an email invitation with your QR code.',
      RegistrationStatus.waitlisted:
          'You\'ve been placed on the waiting list. If a space opens up, you\'ll be automatically moved to confirmed status.',
      RegistrationStatus.rejected:
          'Unfortunately your registration was not accepted for this event. You may have received an email with more details.',
      RegistrationStatus.cancelled: 'Your registration has been cancelled.',
    };
    final text = explanations[_registration.status];
    if (text == null) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppDimens.space4),
      child: Text(
        text,
        textAlign: TextAlign.center,
        style: const TextStyle(
          color: AppColors.textMuted,
          fontSize: 13,
          height: 1.5,
        ),
      ),
    );
  }

  String _formatDate(DateTime dt) {
    final local = dt.toLocal();
    final months = [
      '',
      'Jan',
      'Feb',
      'Mar',
      'Apr',
      'May',
      'Jun',
      'Jul',
      'Aug',
      'Sep',
      'Oct',
      'Nov',
      'Dec'
    ];
    return '${local.day} ${months[local.month]} ${local.year}';
  }

  String _statusLabel(RegistrationStatus status) {
    switch (status) {
      case RegistrationStatus.confirmed:
        return 'CONFIRMED';
      case RegistrationStatus.pendingReview:
        return 'UNDER REVIEW';
      case RegistrationStatus.accepted:
        return 'ACCEPTED';
      case RegistrationStatus.rejected:
        return 'NOT ACCEPTED';
      case RegistrationStatus.waitlisted:
        return 'WAITING LIST';
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
      case RegistrationStatus.pendingReview:
        return PastelBadgeStyle.yellow;
      case RegistrationStatus.accepted:
        return PastelBadgeStyle.blue;
      case RegistrationStatus.rejected:
        return PastelBadgeStyle.pink;
      case RegistrationStatus.waitlisted:
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
      default:
        return status.name;
    }
  }

  _StatusInfo _statusInfo(RegistrationStatus status) {
    switch (status) {
      case RegistrationStatus.confirmed:
        return const _StatusInfo(
          icon: Icons.check_circle_rounded,
          iconBg: AppColors.pastelGreenLight,
          iconColor: AppColors.pastelGreenText,
          bgColor: AppColors.pastelGreenLight,
          textColor: AppColors.pastelGreenText,
          title: 'You\'re In!',
          subtitle:
              'Your registration is confirmed. Check your email for the invitation with your QR code.',
        );
      case RegistrationStatus.pendingReview:
        return const _StatusInfo(
          icon: Icons.hourglass_top_rounded,
          iconBg: AppColors.pastelYellowLight,
          iconColor: AppColors.pastelYellowText,
          bgColor: AppColors.pastelYellowLight,
          textColor: AppColors.pastelYellowText,
          title: 'Under Review',
          subtitle:
              'Your registration has been received and is being reviewed. We\'ll notify you by email.',
        );
      case RegistrationStatus.accepted:
        return const _StatusInfo(
          icon: Icons.hourglass_top_rounded,
          iconBg: AppColors.pastelBlueLight,
          iconColor: AppColors.pastelBlueText,
          bgColor: AppColors.pastelBlueLight,
          textColor: AppColors.pastelBlueText,
          title: 'Accepted — Awaiting a Seat',
          subtitle:
              'Your registration passed review and is waiting for seat allocation. We\'ll notify you when its seat is confirmed.',
        );
      case RegistrationStatus.waitlisted:
        return const _StatusInfo(
          icon: Icons.pending_outlined,
          iconBg: AppColors.pastelBlueLight,
          iconColor: AppColors.pastelBlueText,
          bgColor: AppColors.pastelBlueLight,
          textColor: AppColors.pastelBlueText,
          title: 'On the Waiting List',
          subtitle:
              'You\'re on the waiting list. We\'ll notify you if a spot opens up.',
        );
      case RegistrationStatus.rejected:
        return const _StatusInfo(
          icon: Icons.cancel_outlined,
          iconBg: AppColors.errorBg,
          iconColor: AppColors.error,
          bgColor: AppColors.errorBg,
          textColor: AppColors.error,
          title: 'Not Accepted',
          subtitle:
              'Unfortunately your registration was not accepted for this event.',
        );
      case RegistrationStatus.cancelled:
        return const _StatusInfo(
          icon: Icons.block_rounded,
          iconBg: AppColors.surfaceMuted,
          iconColor: AppColors.textMuted,
          bgColor: AppColors.surfaceMuted,
          textColor: AppColors.textSecondary,
          title: 'Registration Cancelled',
          subtitle: 'This registration has been cancelled.',
        );
      default:
        return const _StatusInfo(
          icon: Icons.help_outline_rounded,
          iconBg: AppColors.surfaceMuted,
          iconColor: AppColors.textMuted,
          bgColor: AppColors.surfaceMuted,
          textColor: AppColors.textSecondary,
          title: 'Status Unknown',
          subtitle: 'We could not determine the status of your registration.',
        );
    }
  }
}

class _StatusInfo {
  final IconData icon;
  final Color iconBg;
  final Color iconColor;
  final Color bgColor;
  final Color textColor;
  final String title;
  final String subtitle;

  const _StatusInfo({
    required this.icon,
    required this.iconBg,
    required this.iconColor,
    required this.bgColor,
    required this.textColor,
    required this.title,
    required this.subtitle,
  });
}
