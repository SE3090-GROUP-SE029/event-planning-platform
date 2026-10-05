import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_empty_state.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../../events/models/event_model.dart';
import '../models/guest_registration_model.dart';
import '../providers/guest_management_providers.dart';
import 'guest_upload_page.dart';
import 'guest_registration_detail_page.dart';
import 'qr_check_in_page.dart';
import 'registration_form_setup_page.dart';

class GuestManagementPage extends ConsumerStatefulWidget {
  final AuthResponseModel? auth;
  final EventModel? event;

  const GuestManagementPage({
    super.key,
    this.auth,
    this.event,
  });

  @override
  ConsumerState<GuestManagementPage> createState() =>
      _GuestManagementPageState();
}

class _GuestManagementPageState extends ConsumerState<GuestManagementPage> {
  AuthResponseModel? _auth;
  EventModel? _event;
  final _scroll = ScrollController();
  final _searchController = TextEditingController();

  // Filter state
  String? _statusFilter;
  String? _rsvpFilter;
  bool? _checkedInFilter;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final routeArgs = ModalRoute.of(context)?.settings.arguments as Map?;
    final routeAuth = routeArgs?['auth'] as AuthResponseModel? ?? widget.auth;
    final routeEvent = routeArgs?['event'] as EventModel? ?? widget.event;

    if (_auth == null || _event == null) {
      _auth = routeAuth;
      _event = routeEvent;
    } else if (_auth != routeAuth || _event != routeEvent) {
      _auth = routeAuth;
      _event = routeEvent;
    }

    final event = _event;
    if (event != null && mounted) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (!mounted) return;
        ref.read(guestListControllerProvider(event.id)).load();
      });
    }
  }

  @override
  void initState() {
    super.initState();
    _scroll.addListener(_onScroll);
  }

  void _onScroll() {
    final event = _event;
    if (event == null) return;
    if (_scroll.position.pixels > _scroll.position.maxScrollExtent - 300) {
      ref.read(guestListControllerProvider(event.id)).load();
    }
  }

  @override
  void dispose() {
    _scroll.dispose();
    _searchController.dispose();
    super.dispose();
  }

  void _applyFilters() {
    final event = _event;
    if (event == null) return;
    final ctrl = ref.read(guestListControllerProvider(event.id));
    ctrl.statusFilter = _statusFilter;
    ctrl.rsvpStatusFilter = _rsvpFilter;
    ctrl.checkedInFilter = _checkedInFilter;
    ctrl.searchQuery = _searchController.text;
    ctrl.load(refresh: true);
  }

  Future<void> _openRegistrationForm(EventModel event) async {
    await Navigator.push<bool>(
      context,
      MaterialPageRoute<bool>(
        builder: (_) => RegistrationFormSetupPage(
          eventId: event.id,
          eventName: event.eventName,
        ),
      ),
    );
    if (!mounted) return;
    ref.invalidate(plannerFormProvider(event.id));
    ref.read(guestListControllerProvider(event.id)).load(refresh: true);
  }

  Future<void> _openGuestUpload(
    EventModel event,
    AuthResponseModel auth,
    bool formIsPublished,
  ) async {
    if (!formIsPublished) {
      await _openRegistrationForm(event);
      return;
    }
    await Navigator.push<void>(
      context,
      MaterialPageRoute<void>(
        builder: (_) => GuestUploadPage(auth: auth, event: event),
      ),
    );
    if (mounted) {
      ref.read(guestListControllerProvider(event.id)).load(refresh: true);
    }
  }

  void _showFilterSheet() {
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppDimens.radiusSheet),
        ),
      ),
      builder: (ctx) => _FilterSheet(
        currentStatus: _statusFilter,
        currentRsvp: _rsvpFilter,
        currentCheckedIn: _checkedInFilter,
        onApply: (status, rsvp, checkedIn) {
          setState(() {
            _statusFilter = status;
            _rsvpFilter = rsvp;
            _checkedInFilter = checkedIn;
          });
          Navigator.pop(ctx);
          _applyFilters();
        },
        onClear: () {
          setState(() {
            _statusFilter = null;
            _rsvpFilter = null;
            _checkedInFilter = null;
          });
          Navigator.pop(ctx);
          _applyFilters();
        },
      ),
    );
  }

  bool get _hasActiveFilter =>
      _statusFilter != null || _rsvpFilter != null || _checkedInFilter != null;

  @override
  Widget build(BuildContext context) {
    final event = _event;
    final auth = _auth;

    if (event == null || auth == null) {
      return const Scaffold(
        backgroundColor: AppColors.canvas,
        body: Center(child: Text('Event not found.')),
      );
    }

    final controller = ref.watch(guestListControllerProvider(event.id));
    final formAsync = ref.watch(plannerFormProvider(event.id));
    final registrationForm = formAsync.asData?.value;
    final formIsPublished = registrationForm?.status == 'PUBLISHED';
    final displayed = controller.filtered;

    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('Guests'),
        actions: [
          IconButton(
            tooltip: 'Registration form settings',
            onPressed: () => _openRegistrationForm(event),
            icon: const Icon(Icons.assignment_outlined),
          ),
          // Filter button
          IconButton(
            tooltip: 'Filter guests',
            onPressed: _showFilterSheet,
            icon: Container(
              width: 38,
              height: 38,
              decoration: BoxDecoration(
                color: _hasActiveFilter
                    ? AppColors.pastelBlueLight
                    : AppColors.surfacePure,
                borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
                border: Border.all(
                  color: _hasActiveFilter
                      ? AppColors.pastelBlue
                      : AppColors.borderSubtle,
                ),
              ),
              child: Icon(
                Icons.filter_list_rounded,
                size: 18,
                color: _hasActiveFilter
                    ? AppColors.pastelBlueText
                    : AppColors.textPrimary,
              ),
            ),
          ),
          // Upload button
          IconButton(
            tooltip: formIsPublished
                ? 'Upload guest list'
                : 'Set up registration form before uploading',
            onPressed: () => _openGuestUpload(event, auth, formIsPublished),
            icon: Container(
              width: 38,
              height: 38,
              decoration: BoxDecoration(
                color: AppColors.surfacePure,
                borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
                border: Border.all(color: AppColors.borderSubtle),
              ),
              child: const Icon(
                Icons.upload_file_rounded,
                size: 18,
                color: AppColors.textPrimary,
              ),
            ),
          ),
          // QR scan check-in button
          IconButton(
            tooltip: 'QR Check-In',
            onPressed: () async {
              await Navigator.push<void>(
                context,
                MaterialPageRoute<void>(
                  builder: (_) => QrCheckInPage(
                    auth: auth,
                    event: event,
                  ),
                ),
              );
              if (mounted) {
                ref
                    .read(guestListControllerProvider(event.id))
                    .load(refresh: true);
              }
            },
            icon: Container(
              width: 38,
              height: 38,
              decoration: BoxDecoration(
                color: AppColors.pastelGreenLight,
                borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
                border: Border.all(color: const Color(0x30C4DDB8)),
              ),
              child: const Icon(
                Icons.qr_code_scanner_rounded,
                size: 18,
                color: AppColors.pastelGreenText,
              ),
            ),
          ),
          const SizedBox(width: AppDimens.space8),
        ],
      ),
      body: Column(
        children: [
          if (formAsync.hasValue && registrationForm?.status != 'PUBLISHED')
            MaterialBanner(
              content: const Text(
                'Publish a registration form before uploading or managing guests.',
              ),
              actions: [
                TextButton(
                  onPressed: () => _openRegistrationForm(event),
                  child: const Text('Set up form'),
                ),
              ],
            ),
          if (formAsync.hasError)
            MaterialBanner(
              content: Text(
                'Could not load the registration form: ${formAsync.error}',
              ),
              actions: [
                TextButton(
                  onPressed: () =>
                      ref.invalidate(plannerFormProvider(event.id)),
                  child: const Text('Retry'),
                ),
              ],
            ),
          // Summary bar
          _buildSummaryBar(controller),
          // Search field
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppDimens.space18,
              AppDimens.space8,
              AppDimens.space18,
              AppDimens.space4,
            ),
            child: TextField(
              controller: _searchController,
              textInputAction: TextInputAction.search,
              onChanged: (v) {
                final ctrl = ref.read(guestListControllerProvider(event.id));
                ctrl.setSearchQuery(v);
              },
              decoration: const InputDecoration(
                hintText: 'Search by name or email…',
                prefixIcon: Icon(Icons.search_rounded,
                    size: 20, color: AppColors.textMuted),
                contentPadding:
                    EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              ),
            ),
          ),
          // Active filter chips
          if (_hasActiveFilter) _buildActiveFilterChips(),
          // Guest list
          Expanded(
            child: _buildBody(
              controller,
              displayed,
              event,
              auth,
              formIsPublished,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSummaryBar(GuestListController controller) {
    final total = controller.total;
    return Container(
      padding: const EdgeInsets.symmetric(
          horizontal: AppDimens.space18, vertical: AppDimens.space10),
      child: Row(
        children: [
          Text(
            '$total guest${total == 1 ? '' : 's'}',
            style: const TextStyle(
              color: AppColors.textSecondary,
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
          ),
          if (controller.loading && controller.guests.isNotEmpty) ...{
            const SizedBox(width: 8),
            const SizedBox(
              width: 12,
              height: 12,
              child: CircularProgressIndicator(strokeWidth: 2),
            ),
          },
        ],
      ),
    );
  }

  Widget _buildActiveFilterChips() {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(
          horizontal: AppDimens.space18, vertical: AppDimens.space4),
      child: Row(
        children: [
          if (_statusFilter != null)
            _filterChip(
              label: 'Status: $_statusFilter',
              onRemove: () {
                setState(() => _statusFilter = null);
                _applyFilters();
              },
            ),
          if (_rsvpFilter != null)
            _filterChip(
              label: 'RSVP: $_rsvpFilter',
              onRemove: () {
                setState(() => _rsvpFilter = null);
                _applyFilters();
              },
            ),
          if (_checkedInFilter != null)
            _filterChip(
              label: _checkedInFilter! ? 'Checked In' : 'Not Checked In',
              onRemove: () {
                setState(() => _checkedInFilter = null);
                _applyFilters();
              },
            ),
        ],
      ),
    );
  }

  Widget _filterChip({required String label, required VoidCallback onRemove}) {
    return Padding(
      padding: const EdgeInsets.only(right: 6),
      child: Chip(
        label: Text(label,
            style: const TextStyle(
                fontSize: 12,
                color: AppColors.pastelBlueText,
                fontWeight: FontWeight.w600)),
        backgroundColor: AppColors.pastelBlueLight,
        deleteIcon: const Icon(Icons.close_rounded,
            size: 14, color: AppColors.pastelBlueText),
        onDeleted: onRemove,
        padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 0),
        materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
      ),
    );
  }

  Widget _buildBody(
    GuestListController controller,
    List<PlannerRegistrationModel> displayed,
    EventModel event,
    AuthResponseModel auth,
    bool formIsPublished,
  ) {
    if (controller.loading && controller.guests.isEmpty) {
      return const Center(
        child: CircularProgressIndicator(
          valueColor: AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
        ),
      );
    }
    if (controller.hasError && controller.guests.isEmpty) {
      return ListView(
        padding: const EdgeInsets.all(AppDimens.space20),
        children: [
          const SizedBox(height: 60),
          PastelEmptyState(
            icon: Icons.error_outline_rounded,
            iconVariant: PastelIconVariant.pink,
            title: 'Unable to load guests',
            description:
                controller.error ?? 'Something went wrong. Please try again.',
            actionLabel: 'Retry',
            onActionTap: () => controller.load(refresh: true),
          ),
        ],
      );
    }
    if (displayed.isEmpty && controller.guests.isEmpty) {
      return ListView(
        padding: const EdgeInsets.all(AppDimens.space20),
        children: [
          const SizedBox(height: 60),
          PastelEmptyState(
            icon: Icons.people_outline_rounded,
            iconVariant: PastelIconVariant.yellow,
            title: 'No guests yet',
            description: formIsPublished
                ? 'Upload a guest list or share your registration form link to get started.'
                : 'Set up and publish a registration form before uploading guests.',
            actionLabel: formIsPublished
                ? 'Upload guest list'
                : 'Set up registration form',
            onActionTap: () => _openGuestUpload(event, auth, formIsPublished),
          ),
        ],
      );
    }
    if (displayed.isEmpty && _searchController.text.isNotEmpty) {
      return ListView(
        padding: const EdgeInsets.all(AppDimens.space20),
        children: const [
          SizedBox(height: 60),
          PastelEmptyState(
            icon: Icons.search_off_rounded,
            iconVariant: PastelIconVariant.blue,
            title: 'No results',
            description:
                'No guests match your search. Try a different name or email.',
          ),
        ],
      );
    }

    return RefreshIndicator(
      color: AppColors.obsidianBlack,
      backgroundColor: AppColors.surfacePure,
      onRefresh: () => controller.load(refresh: true),
      child: ListView.builder(
        controller: _scroll,
        padding: const EdgeInsets.fromLTRB(
          AppDimens.space18,
          AppDimens.space4,
          AppDimens.space18,
          AppDimens.space24,
        ),
        itemCount: displayed.length + (controller.loadingMore ? 1 : 0),
        itemBuilder: (_, index) {
          if (index == displayed.length) {
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(AppDimens.space16),
                child: CircularProgressIndicator(
                  valueColor:
                      AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
                ),
              ),
            );
          }
          final guest = displayed[index];
          return Padding(
            padding: const EdgeInsets.only(bottom: AppDimens.space10),
            child: _GuestCard(
              guest: guest,
              onTap: () async {
                await Navigator.push<void>(
                  context,
                  MaterialPageRoute<void>(
                    builder: (_) => GuestRegistrationDetailPage(
                      auth: auth,
                      event: event,
                      registrationId: guest.id,
                    ),
                  ),
                );
                if (mounted) {
                  ref
                      .read(guestListControllerProvider(event.id))
                      .load(refresh: true);
                }
              },
            ),
          );
        },
      ),
    );
  }
}

// ──────────────────────────────────────────────────────
// Guest card widget
// ──────────────────────────────────────────────────────

class _GuestCard extends StatelessWidget {
  final PlannerRegistrationModel guest;
  final VoidCallback onTap;

  const _GuestCard({required this.guest, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return PastelCard(
      padding: const EdgeInsets.all(AppDimens.space16),
      onTap: onTap,
      child: Row(
        children: [
          // Avatar / icon
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: _avatarColor(guest.status),
              borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
            ),
            alignment: Alignment.center,
            child: Text(
              guest.fullName.isNotEmpty ? guest.fullName[0].toUpperCase() : '?',
              style: TextStyle(
                color: _avatarTextColor(guest.status),
                fontSize: 18,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
          const SizedBox(width: AppDimens.space12),
          // Name and email
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  guest.fullName,
                  style: const TextStyle(
                    color: AppColors.textPrimary,
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    letterSpacing: -0.2,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 2),
                Text(
                  guest.emailAddress,
                  style: const TextStyle(
                    color: AppColors.textSecondary,
                    fontSize: 12,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 6),
                Wrap(
                  spacing: 6,
                  runSpacing: 4,
                  children: [
                    PastelPillBadge(
                      text: _statusLabel(guest.status),
                      style: _statusBadgeStyle(guest.status),
                      fontSize: 10,
                      padding: const EdgeInsets.symmetric(
                          horizontal: 7, vertical: 3),
                    ),
                    if (guest.rsvpStatus != null &&
                        guest.rsvpStatus != RsvpStatus.notResponded)
                      PastelPillBadge(
                        text: 'RSVP: ${_rsvpLabel(guest.rsvpStatus!)}',
                        style: _rsvpBadgeStyle(guest.rsvpStatus!),
                        fontSize: 10,
                        padding: const EdgeInsets.symmetric(
                            horizontal: 7, vertical: 3),
                      ),
                    if (guest.isCheckedIn)
                      const PastelPillBadge(
                        text: '✓ Checked In',
                        style: PastelBadgeStyle.green,
                        fontSize: 10,
                        padding:
                            EdgeInsets.symmetric(horizontal: 7, vertical: 3),
                      ),
                  ],
                ),
              ],
            ),
          ),
          const Icon(
            Icons.chevron_right_rounded,
            color: AppColors.textMuted,
            size: 20,
          ),
        ],
      ),
    );
  }

  Color _avatarColor(RegistrationStatus status) {
    switch (status) {
      case RegistrationStatus.confirmed:
        return AppColors.pastelGreenLight;
      case RegistrationStatus.pendingReview:
      case RegistrationStatus.accepted:
        return AppColors.pastelYellowLight;
      case RegistrationStatus.rejected:
        return AppColors.errorBg;
      case RegistrationStatus.waitlisted:
        return AppColors.pastelBlueLight;
      case RegistrationStatus.cancelled:
        return AppColors.surfaceMuted;
      default:
        return AppColors.surfaceMuted;
    }
  }

  Color _avatarTextColor(RegistrationStatus status) {
    switch (status) {
      case RegistrationStatus.confirmed:
        return AppColors.pastelGreenText;
      case RegistrationStatus.pendingReview:
        return AppColors.pastelYellowText;
      case RegistrationStatus.accepted:
        return AppColors.pastelBlueText;
      case RegistrationStatus.rejected:
        return AppColors.error;
      case RegistrationStatus.waitlisted:
        return AppColors.pastelBlueText;
      default:
        return AppColors.textSecondary;
    }
  }

  String _statusLabel(RegistrationStatus status) {
    switch (status) {
      case RegistrationStatus.confirmed:
        return 'CONFIRMED';
      case RegistrationStatus.pendingReview:
        return 'PENDING REVIEW';
      case RegistrationStatus.accepted:
        return 'ACCEPTED';
      case RegistrationStatus.rejected:
        return 'REJECTED';
      case RegistrationStatus.waitlisted:
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
      case RsvpStatus.attended:
        return PastelBadgeStyle.oliveRibbon;
      default:
        return PastelBadgeStyle.neutral;
    }
  }
}

// ──────────────────────────────────────────────────────
// Filter bottom sheet
// ──────────────────────────────────────────────────────

class _FilterSheet extends StatefulWidget {
  final String? currentStatus;
  final String? currentRsvp;
  final bool? currentCheckedIn;
  final void Function(String? status, String? rsvp, bool? checkedIn) onApply;
  final VoidCallback onClear;

  const _FilterSheet({
    this.currentStatus,
    this.currentRsvp,
    this.currentCheckedIn,
    required this.onApply,
    required this.onClear,
  });

  @override
  State<_FilterSheet> createState() => _FilterSheetState();
}

class _FilterSheetState extends State<_FilterSheet> {
  String? _status;
  String? _rsvp;
  bool? _checkedIn;

  static const _statuses = [
    'CONFIRMED',
    'PENDING_REVIEW',
    'ACCEPTED',
    'REJECTED',
    'WAITLISTED',
    'CANCELLED',
  ];
  static const _rsvps = [
    'ACCEPTED',
    'DECLINED',
    'MAYBE',
    'NOT_RESPONDED',
  ];

  @override
  void initState() {
    super.initState();
    _status = widget.currentStatus;
    _rsvp = widget.currentRsvp;
    _checkedIn = widget.currentCheckedIn;
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(
        bottom: MediaQuery.of(context).viewInsets.bottom,
      ),
      child: SingleChildScrollView(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(AppDimens.space20,
              AppDimens.space24, AppDimens.space20, AppDimens.space32),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Filter Guests',
                    style: TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary,
                      letterSpacing: -0.4,
                    ),
                  ),
                  TextButton(
                    onPressed: widget.onClear,
                    child: const Text('Clear all',
                        style: TextStyle(color: AppColors.textSecondary)),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space20),
              const PastelSectionHeader(
                title: 'Registration Status',
                padding: EdgeInsets.only(bottom: AppDimens.space10),
              ),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: _statuses.map((s) {
                  final selected = _status == s;
                  return FilterChip(
                    label: Text(s),
                    selected: selected,
                    onSelected: (_) =>
                        setState(() => _status = selected ? null : s),
                    selectedColor: AppColors.pastelBlueLight,
                    checkmarkColor: AppColors.pastelBlueText,
                    labelStyle: TextStyle(
                      color: selected
                          ? AppColors.pastelBlueText
                          : AppColors.textPrimary,
                      fontWeight: FontWeight.w600,
                      fontSize: 12,
                    ),
                  );
                }).toList(),
              ),
              const SizedBox(height: AppDimens.space16),
              const PastelSectionHeader(
                title: 'RSVP Status',
                padding: EdgeInsets.only(bottom: AppDimens.space10),
              ),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: _rsvps.map((r) {
                  final selected = _rsvp == r;
                  return FilterChip(
                    label: Text(r),
                    selected: selected,
                    onSelected: (_) =>
                        setState(() => _rsvp = selected ? null : r),
                    selectedColor: AppColors.pastelGreenLight,
                    checkmarkColor: AppColors.pastelGreenText,
                    labelStyle: TextStyle(
                      color: selected
                          ? AppColors.pastelGreenText
                          : AppColors.textPrimary,
                      fontWeight: FontWeight.w600,
                      fontSize: 12,
                    ),
                  );
                }).toList(),
              ),
              const SizedBox(height: AppDimens.space16),
              const PastelSectionHeader(
                title: 'Check-In',
                padding: EdgeInsets.only(bottom: AppDimens.space10),
              ),
              Wrap(
                spacing: 8,
                children: [
                  FilterChip(
                    label: const Text('Checked In'),
                    selected: _checkedIn == true,
                    onSelected: (_) => setState(
                        () => _checkedIn = _checkedIn == true ? null : true),
                    selectedColor: AppColors.pastelGreenLight,
                    checkmarkColor: AppColors.pastelGreenText,
                  ),
                  FilterChip(
                    label: const Text('Not Checked In'),
                    selected: _checkedIn == false,
                    onSelected: (_) => setState(
                        () => _checkedIn = _checkedIn == false ? null : false),
                    selectedColor: AppColors.pastelYellowLight,
                    checkmarkColor: AppColors.pastelYellowText,
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space24),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: () => widget.onApply(_status, _rsvp, _checkedIn),
                  child: const Text('Apply Filters'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
