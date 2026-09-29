import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../core/theme/app_theme.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../api/guest_management_remote_datasource.dart';
import '../models/registration_form_model.dart';
import 'registration_status_page.dart';

/// Public-facing guest registration page.
/// Fetches the dynamic form from backend by public form ID.
/// Submits guest details and custom Q&A to the backend.
class GuestRegistrationPage extends StatefulWidget {
  final String publicFormId;

  const GuestRegistrationPage({super.key, required this.publicFormId});

  @override
  State<GuestRegistrationPage> createState() => _GuestRegistrationPageState();
}

class _GuestRegistrationPageState extends State<GuestRegistrationPage> {
  final _api = GuestManagementRemoteDataSource();
  final _formKey = GlobalKey<FormState>();

  // Core fields (always required by backend)
  final _fullNameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _orgCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();

  // Dynamic question answers — key = questionId
  final Map<String, TextEditingController> _answerControllers = {};

  // Page state
  PublicFormModel? _form;
  bool _loading = true;
  String? _loadError;
  bool _submitting = false;
  String? _submitError;

  @override
  void initState() {
    super.initState();
    _loadForm();
  }

  @override
  void dispose() {
    _fullNameCtrl.dispose();
    _emailCtrl.dispose();
    _orgCtrl.dispose();
    _phoneCtrl.dispose();
    for (final c in _answerControllers.values) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _loadForm() async {
    setState(() {
      _loading = true;
      _loadError = null;
    });
    try {
      final form = await _api.getPublicForm(widget.publicFormId);
      if (mounted) {
        // Pre-create controllers for each custom question
        for (final q in form.questions) {
          _answerControllers[q.id] = TextEditingController();
        }
        setState(() {
          _form = form;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loadError = _parseError(e);
          _loading = false;
        });
      }
    }
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    if (_form == null) return;

    // Build answers list
    final answers = <Map<String, dynamic>>[];
    for (final q in _form!.questions) {
      final ctrl = _answerControllers[q.id];
      if (ctrl != null && ctrl.text.trim().isNotEmpty) {
        answers.add({
          'questionId': q.id,
          'answer': ctrl.text.trim(),
        });
      }
    }

    setState(() {
      _submitting = true;
      _submitError = null;
    });

    try {
      final result = await _api.submitRegistration(
        widget.publicFormId,
        fullName: _fullNameCtrl.text.trim(),
        emailAddress: _emailCtrl.text.trim(),
        organisation: _orgCtrl.text.trim().isEmpty ? null : _orgCtrl.text.trim(),
        phoneNumber: _phoneCtrl.text.trim().isEmpty ? null : _phoneCtrl.text.trim(),
        answers: answers.isEmpty ? null : answers,
      );
      if (mounted) {
        // Navigate to status page with result
        Navigator.pushReplacement(
          context,
          MaterialPageRoute<void>(
            builder: (_) => RegistrationStatusPage(
              registration: result,
              eventName: _form!.eventName,
            ),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _submitting = false;
          _submitError = _parseError(e);
        });
      }
    }
  }

  String _parseError(Object e) {
    final msg = e.toString();
    final jsonMatch =
        RegExp(r'"(?:message|error)"\s*:\s*"([^"]+)"').firstMatch(msg);
    if (jsonMatch != null) return jsonMatch.group(1)!;
    if (msg.contains('form_closed')) return 'This registration form is currently closed.';
    if (msg.contains('form_not_found')) return 'Registration form not found.';
    if (msg.contains('seat_limit')) return 'This event has reached its capacity. You may be placed on the waiting list.';
    if (msg.contains('already_registered')) return 'You are already registered for this event.';
    if (msg.contains('400')) return 'Invalid data. Please check your inputs.';
    if (msg.contains('404')) return 'Registration form not found.';
    return 'Something went wrong. Please try again.';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: Text(
          _form?.eventName ?? 'Register',
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
        ),
      ),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(
                valueColor:
                    AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
              ),
            )
          : _loadError != null
              ? _buildLoadError()
              : _buildForm(),
    );
  }

  Widget _buildLoadError() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppDimens.space24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.link_off_rounded,
                size: 48, color: AppColors.error),
            const SizedBox(height: 16),
            Text(
              _loadError!,
              textAlign: TextAlign.center,
              style: const TextStyle(
                color: AppColors.textSecondary,
                fontSize: 15,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: _loadForm,
              icon: const Icon(Icons.refresh_rounded, size: 18),
              label: const Text('Retry'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildForm() {
    final form = _form!;
    final now = DateTime.now().toUtc();
    final isClosed = !form.isOpen || now.isAfter(form.closesAt);

    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppDimens.space20,
          AppDimens.space12,
          AppDimens.space20,
          AppDimens.space32,
        ),
        children: [
          // Event info card
          _buildEventInfoCard(form),
          const SizedBox(height: AppDimens.space4),

          if (isClosed) ...[
            _buildClosedBanner(form),
          ] else ...[
            // Required fields section
            const PastelSectionHeader(title: 'Your Details'),

            PastelCard(
              padding: const EdgeInsets.all(AppDimens.space18),
              child: Column(
                children: [
                  _buildTextField(
                    controller: _fullNameCtrl,
                    label: 'Full Name',
                    hint: 'Enter your full name',
                    icon: Icons.person_outline_rounded,
                    required: true,
                    validator: (v) {
                      if (v == null || v.trim().isEmpty) {
                        return 'Full name is required';
                      }
                      if (v.trim().length > 200) {
                        return 'Name must be under 200 characters';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: AppDimens.space14),
                  _buildTextField(
                    controller: _emailCtrl,
                    label: 'Email Address',
                    hint: 'Enter your email',
                    icon: Icons.email_outlined,
                    required: true,
                    keyboardType: TextInputType.emailAddress,
                    validator: (v) {
                      if (v == null || v.trim().isEmpty) {
                        return 'Email is required';
                      }
                      final emailRegex = RegExp(
                          r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$');
                      if (!emailRegex.hasMatch(v.trim())) {
                        return 'Enter a valid email address';
                      }
                      return null;
                    },
                  ),
                  // Optional fields
                  if (form.optionalFields.contains('organisation')) ...[
                    const SizedBox(height: AppDimens.space14),
                    _buildTextField(
                      controller: _orgCtrl,
                      label: 'Organisation',
                      hint: 'Your company or organisation (optional)',
                      icon: Icons.business_outlined,
                      required: false,
                    ),
                  ],
                  if (form.optionalFields.contains('phoneNumber')) ...[
                    const SizedBox(height: AppDimens.space14),
                    _buildTextField(
                      controller: _phoneCtrl,
                      label: 'Phone Number',
                      hint: 'Your phone number (optional)',
                      icon: Icons.phone_outlined,
                      required: false,
                      keyboardType: TextInputType.phone,
                    ),
                  ],
                ],
              ),
            ),

            // Custom questions
            if (form.questions.isNotEmpty) ...[
              const PastelSectionHeader(title: 'Additional Questions'),
              PastelCard(
                padding: const EdgeInsets.all(AppDimens.space18),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: form.questions.asMap().entries.map((entry) {
                    final index = entry.key;
                    final question = entry.value;
                    final ctrl = _answerControllers[question.id]!;
                    return Padding(
                      padding: EdgeInsets.only(
                          bottom: index < form.questions.length - 1
                              ? AppDimens.space16
                              : 0),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Text(
                                  question.question,
                                  style: const TextStyle(
                                    color: AppColors.textPrimary,
                                    fontSize: 14,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ),
                              if (question.required)
                                const Padding(
                                  padding: EdgeInsets.only(left: 4),
                                  child: Text(
                                    '*',
                                    style: TextStyle(color: AppColors.error),
                                  ),
                                ),
                            ],
                          ),
                          const SizedBox(height: 8),
                          TextFormField(
                            controller: ctrl,
                            maxLines: 2,
                            textInputAction: TextInputAction.next,
                            decoration: InputDecoration(
                              hintText: question.required
                                  ? 'Your answer (required)'
                                  : 'Your answer (optional)',
                            ),
                            validator: question.required
                                ? (v) {
                                    if (v == null || v.trim().isEmpty) {
                                      return 'This answer is required';
                                    }
                                    return null;
                                  }
                                : null,
                          ),
                        ],
                      ),
                    );
                  }).toList(),
                ),
              ),
            ],

            // Error
            if (_submitError != null) ...[
              const SizedBox(height: AppDimens.space12),
              _buildErrorBanner(),
            ],

            const SizedBox(height: AppDimens.space20),

            // Submit button
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                style: AppButtonStyles.primary(),
                onPressed: _submitting ? null : _submit,
                icon: _submitting
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          valueColor: AlwaysStoppedAnimation<Color>(
                              Colors.white),
                        ),
                      )
                    : const Icon(Icons.send_rounded, size: 18),
                label: Text(
                    _submitting ? 'Submitting…' : 'Submit Registration'),
              ),
            ),
            const SizedBox(height: AppDimens.space12),
            const Text(
              'By submitting, you agree that your information will be used for event management purposes. You may receive an email with your registration status.',
              textAlign: TextAlign.center,
              style: TextStyle(
                color: AppColors.textMuted,
                fontSize: 11,
                height: 1.4,
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildEventInfoCard(PublicFormModel form) {
    return PastelCard(
      padding: const EdgeInsets.all(AppDimens.space20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
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
                      form.eventName,
                      style: const TextStyle(
                        color: AppColors.textPrimary,
                        fontSize: 18,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.3,
                      ),
                    ),
                    if (form.location != null && form.location!.isNotEmpty)
                      Text(
                        form.location!,
                        style: const TextStyle(
                          color: AppColors.textSecondary,
                          fontSize: 13,
                        ),
                      ),
                  ],
                ),
              ),
              PastelPillBadge(
                text: form.isOpen ? 'OPEN' : 'CLOSED',
                style: form.isOpen ? PastelBadgeStyle.green : PastelBadgeStyle.pink,
              ),
            ],
          ),
          if (form.description != null && form.description!.isNotEmpty) ...[
            const SizedBox(height: AppDimens.space12),
            Text(
              form.description!,
              style: const TextStyle(
                color: AppColors.textSecondary,
                fontSize: 13,
                height: 1.4,
              ),
            ),
          ],
          const SizedBox(height: AppDimens.space14),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _infoBadge(
                Icons.calendar_today_outlined,
                _formatDate(form.startsAt),
              ),
              _infoBadge(
                Icons.chair_outlined,
                '${form.seatLimit} seats',
              ),
              _infoBadge(
                Icons.timer_outlined,
                'Closes ${_formatDate(form.closesAt)}',
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildClosedBanner(PublicFormModel form) {
    return PastelCard(
      backgroundColor: AppColors.errorBg,
      padding: const EdgeInsets.all(AppDimens.space20),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.lock_outlined, color: AppColors.error, size: 24),
          const SizedBox(width: AppDimens.space12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Registration Closed',
                  style: TextStyle(
                    color: AppColors.error,
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  form.isOpen
                      ? 'Registration for this event has ended.'
                      : 'Registration has not opened yet or is closed.',
                  style: const TextStyle(
                    color: AppColors.error,
                    fontSize: 13,
                    height: 1.4,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildErrorBanner() {
    return PastelCard(
      backgroundColor: AppColors.errorBg,
      padding: const EdgeInsets.all(AppDimens.space14),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.error_outline_rounded,
              color: AppColors.error, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              _submitError!,
              style: const TextStyle(
                color: AppColors.error,
                fontSize: 13,
                height: 1.3,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    required String hint,
    required IconData icon,
    required bool required,
    TextInputType? keyboardType,
    String? Function(String?)? validator,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Text(
              label,
              style: const TextStyle(
                color: AppColors.textPrimary,
                fontSize: 13,
                fontWeight: FontWeight.w700,
              ),
            ),
            if (required)
              const Text(' *', style: TextStyle(color: AppColors.error)),
          ],
        ),
        const SizedBox(height: 6),
        TextFormField(
          controller: controller,
          keyboardType: keyboardType,
          textInputAction: TextInputAction.next,
          decoration: InputDecoration(
            hintText: hint,
            prefixIcon: Icon(icon, size: 18, color: AppColors.textMuted),
          ),
          validator: validator ??
              (required
                  ? (v) {
                      if (v == null || v.trim().isEmpty) {
                        return '$label is required';
                      }
                      return null;
                    }
                  : null),
        ),
      ],
    );
  }

  Widget _infoBadge(IconData icon, String text) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: AppColors.surfaceMuted,
        borderRadius: BorderRadius.circular(AppDimens.radiusSmall),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 13, color: AppColors.textSecondary),
          const SizedBox(width: 5),
          Text(
            text,
            style: const TextStyle(
              color: AppColors.textPrimary,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }

  String _formatDate(DateTime dt) {
    final local = dt.toLocal();
    final months = [
      '', 'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    return '${local.day} ${months[local.month]} ${local.year}';
  }
}
