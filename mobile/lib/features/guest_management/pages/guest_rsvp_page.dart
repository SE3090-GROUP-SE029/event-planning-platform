import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../core/theme/app_theme.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../api/guest_management_remote_datasource.dart';
import '../models/guest_registration_model.dart';

/// Public RSVP page.
/// Guest uses their invitation secret to respond (ACCEPTED / DECLINED / MAYBE).
/// The secret is obtained from the registration status page.
class GuestRsvpPage extends StatefulWidget {
  final PublicRegistrationModel registration;
  final String eventName;

  const GuestRsvpPage({
    super.key,
    required this.registration,
    required this.eventName,
  });

  @override
  State<GuestRsvpPage> createState() => _GuestRsvpPageState();
}

class _GuestRsvpPageState extends State<GuestRsvpPage> {
  final _api = GuestManagementRemoteDataSource();

  // The secret is saved in statusSecret from the original submission response.
  // On RSVP, we also need to allow the user to input it if they're coming
  // from a status check (statusSecret is null on status checks).
  final _secretController = TextEditingController();

  String? _selectedResponse;
  bool _submitting = false;
  String? _error;
  PublicRegistrationModel? _result;

  static const _options = [
    _RsvpOption(
      value: 'ACCEPTED',
      label: 'Accept',
      description: 'I will attend this event',
      icon: Icons.check_circle_outline_rounded,
      variant: PastelIconVariant.green,
      badgeStyle: PastelBadgeStyle.green,
    ),
    _RsvpOption(
      value: 'DECLINED',
      label: 'Decline',
      description: 'I cannot make it',
      icon: Icons.cancel_outlined,
      variant: PastelIconVariant.pink,
      badgeStyle: PastelBadgeStyle.pink,
    ),
    _RsvpOption(
      value: 'MAYBE',
      label: 'Maybe',
      description: 'I\'m not sure yet',
      icon: Icons.help_outline_rounded,
      variant: PastelIconVariant.yellow,
      badgeStyle: PastelBadgeStyle.yellow,
    ),
  ];

  @override
  void initState() {
    super.initState();
    // Pre-fill secret if available from initial submission
    if (widget.registration.statusSecret != null) {
      _secretController.text = widget.registration.statusSecret!;
    }
  }

  @override
  void dispose() {
    _secretController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_selectedResponse == null) {
      setState(() => _error = 'Please select an RSVP response.');
      return;
    }
    final secret = _secretController.text.trim();
    if (secret.isEmpty) {
      setState(() => _error = 'Your registration secret is required to submit RSVP.');
      return;
    }
    if (secret.length != 43) {
      setState(() => _error = 'Invalid secret format. Please use the secret from your confirmation email.');
      return;
    }

    setState(() {
      _submitting = true;
      _error = null;
    });

    try {
      final result = await _api.submitRsvp(
        widget.registration.publicReference,
        secret,
        _selectedResponse!,
      );
      if (mounted) {
        setState(() {
          _result = result;
          _submitting = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = _parseError(e);
          _submitting = false;
        });
      }
    }
  }

  String _parseError(Object e) {
    final msg = e.toString();
    final jsonMatch =
        RegExp(r'"(?:message|error)"\s*:\s*"([^"]+)"').firstMatch(msg);
    if (jsonMatch != null) return jsonMatch.group(1)!;
    if (msg.contains('invalid_rsvp')) {
      return 'Invalid RSVP response. Please select Accepted, Declined, or Maybe.';
    }
    if (msg.contains('403') || msg.contains('not_found')) {
      return 'Invalid secret or registration not found. Please check your confirmation email.';
    }
    if (msg.contains('409')) {
      return 'RSVP has already been submitted.';
    }
    return 'Failed to submit RSVP. Please try again.';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(title: const Text('RSVP')),
      body: _result != null ? _buildSuccess() : _buildForm(),
    );
  }

  Widget _buildSuccess() {
    final response = _result!.rsvpStatus;
    final rsvpLabel = response == RsvpStatus.accepted
        ? 'Accepted'
        : response == RsvpStatus.declined
            ? 'Declined'
            : 'Maybe';
    final icon = response == RsvpStatus.accepted
        ? Icons.check_circle_rounded
        : response == RsvpStatus.declined
            ? Icons.cancel_rounded
            : Icons.help_rounded;
    final variant = response == RsvpStatus.accepted
        ? PastelIconVariant.green
        : response == RsvpStatus.declined
            ? PastelIconVariant.pink
            : PastelIconVariant.yellow;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppDimens.space32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            PastelIconBadge(
              icon: icon,
              variant: variant,
              size: 80,
              iconSize: 40,
            ),
            const SizedBox(height: AppDimens.space20),
            const Text(
              'RSVP Submitted!',
              style: TextStyle(
                color: AppColors.textPrimary,
                fontSize: 24,
                fontWeight: FontWeight.w800,
                letterSpacing: -0.5,
              ),
            ),
            const SizedBox(height: AppDimens.space8),
            Text(
              'Your response: $rsvpLabel',
              style: const TextStyle(
                color: AppColors.textSecondary,
                fontSize: 15,
              ),
            ),
            const SizedBox(height: AppDimens.space16),
            Text(
              widget.eventName,
              textAlign: TextAlign.center,
              style: const TextStyle(
                color: AppColors.textMuted,
                fontSize: 14,
              ),
            ),
            const SizedBox(height: AppDimens.space32),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () {
                  // Return the updated registration to the caller
                  Navigator.pop(context, _result);
                },
                child: const Text('Done'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildForm() {
    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppDimens.space20,
        AppDimens.space16,
        AppDimens.space20,
        AppDimens.space32,
      ),
      children: [
        // Event context
        PastelCard(
          padding: const EdgeInsets.all(AppDimens.space16),
          child: Row(
            children: [
              const PastelIconBadge(
                icon: Icons.event_rounded,
                variant: PastelIconVariant.blue,
                size: 44,
                iconSize: 22,
              ),
              const SizedBox(width: AppDimens.space12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      widget.eventName,
                      style: const TextStyle(
                        color: AppColors.textPrimary,
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const Text(
                      'Respond to your invitation',
                      style: TextStyle(
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

        const SizedBox(height: AppDimens.space20),
        const Text(
          'Will you attend?',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontSize: 20,
            fontWeight: FontWeight.w800,
            letterSpacing: -0.4,
          ),
        ),
        const SizedBox(height: AppDimens.space4),
        const Text(
          'Select your response below.',
          style: TextStyle(color: AppColors.textSecondary, fontSize: 14),
        ),
        const SizedBox(height: AppDimens.space16),

        // RSVP options
        ..._options.map((option) => _buildRsvpOption(option)),

        const SizedBox(height: AppDimens.space20),

        // Secret field (required for RSVP validation)
        if (widget.registration.statusSecret == null) ...[
          const Text(
            'Registration Secret',
            style: TextStyle(
              color: AppColors.textPrimary,
              fontSize: 14,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 4),
          const Text(
            'Enter the secret from your confirmation email.',
            style: TextStyle(color: AppColors.textSecondary, fontSize: 12),
          ),
          const SizedBox(height: 8),
          TextField(
            controller: _secretController,
            decoration: const InputDecoration(
              hintText: 'Paste your registration secret…',
              prefixIcon: Icon(
                Icons.vpn_key_outlined,
                size: 18,
                color: AppColors.textMuted,
              ),
            ),
            maxLength: 43,
            buildCounter: (_, {required currentLength, required isFocused, maxLength}) =>
                null,
          ),
        ] else ...[
          // Show confirmation that we have the secret
          const PastelCard(
            backgroundColor: AppColors.pastelBlueLight,
            padding: EdgeInsets.all(AppDimens.space12),
            child: Row(
              children: [
                Icon(Icons.lock_outline_rounded,
                    size: 16, color: AppColors.pastelBlueText),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Your registration identity has been verified automatically.',
                    style: TextStyle(
                      color: AppColors.pastelBlueText,
                      fontSize: 12,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],

        // Error
        if (_error != null) ...[
          const SizedBox(height: AppDimens.space12),
          Container(
            padding: const EdgeInsets.all(AppDimens.space12),
            decoration: BoxDecoration(
              color: AppColors.errorBg,
              borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
            ),
            child: Row(
              children: [
                const Icon(Icons.error_outline_rounded,
                    color: AppColors.error, size: 18),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    _error!,
                    style: const TextStyle(
                      color: AppColors.error,
                      fontSize: 13,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],

        const SizedBox(height: AppDimens.space24),

        SizedBox(
          width: double.infinity,
          child: ElevatedButton.icon(
            style: AppButtonStyles.primary(),
            onPressed: _submitting || _selectedResponse == null ? null : _submit,
            icon: _submitting
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      valueColor:
                          AlwaysStoppedAnimation<Color>(Colors.white),
                    ),
                  )
                : const Icon(Icons.how_to_vote_rounded, size: 18),
            label: Text(_submitting ? 'Submitting…' : 'Submit RSVP'),
          ),
        ),
      ],
    );
  }

  Widget _buildRsvpOption(_RsvpOption option) {
    final selected = _selectedResponse == option.value;
    return Padding(
      padding: const EdgeInsets.only(bottom: AppDimens.space10),
      child: GestureDetector(
        onTap: () => setState(() => _selectedResponse = option.value),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 180),
          padding: const EdgeInsets.all(AppDimens.space16),
          decoration: BoxDecoration(
            color: selected ? AppColors.surfacePure : AppColors.surfacePure,
            borderRadius: BorderRadius.circular(AppDimens.radiusCard),
            border: Border.all(
              color: selected ? AppColors.obsidianBlack : AppColors.borderSubtle,
              width: selected ? 2 : 1,
            ),
            boxShadow: selected
                ? [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.06),
                      blurRadius: 8,
                      offset: const Offset(0, 2),
                    ),
                  ]
                : [],
          ),
          child: Row(
            children: [
              PastelIconBadge(
                icon: option.icon,
                variant: option.variant,
                size: 44,
                iconSize: 22,
              ),
              const SizedBox(width: AppDimens.space12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      option.label,
                      style: TextStyle(
                        color: AppColors.textPrimary,
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                        letterSpacing: selected ? -0.2 : 0,
                      ),
                    ),
                    Text(
                      option.description,
                      style: const TextStyle(
                        color: AppColors.textSecondary,
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ),
              if (selected)
                const Icon(
                  Icons.check_circle_rounded,
                  color: AppColors.obsidianBlack,
                  size: 22,
                )
              else
                const Icon(
                  Icons.radio_button_unchecked_rounded,
                  color: AppColors.borderMuted,
                  size: 22,
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _RsvpOption {
  final String value;
  final String label;
  final String description;
  final IconData icon;
  final PastelIconVariant variant;
  final PastelBadgeStyle badgeStyle;

  const _RsvpOption({
    required this.value,
    required this.label,
    required this.description,
    required this.icon,
    required this.variant,
    required this.badgeStyle,
  });
}
