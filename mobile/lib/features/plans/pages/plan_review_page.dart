import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../core/theme/app_theme.dart';
import '../../../shared/widgets/pastel_bottom_nav_bar.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/providers/auth_providers.dart';
import '../models/plan_model.dart';
import '../providers/plan_providers.dart';
import '../widgets/timeline_phase_card.dart';

class PlanReviewPage extends ConsumerStatefulWidget {
  final String planId;
  const PlanReviewPage({super.key, required this.planId});

  @override
  ConsumerState<PlanReviewPage> createState() => _PlanReviewPageState();
}

class _PlanReviewPageState extends ConsumerState<PlanReviewPage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final session = ref.read(currentUserProvider);
      if (session != null) {
        ref
            .read(planProvider((token: session.accessToken, planId: widget.planId)))
            .load();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(currentUserProvider);
    if (session == null) {
      return const Scaffold(
        backgroundColor: AppColors.canvas,
        body: Center(child: Text('Please sign in to review plans.')),
      );
    }
    final isAdmin = session.roles.contains('ADMIN');
    final controller = ref.watch(
      planProvider((token: session.accessToken, planId: widget.planId)),
    );

    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('AI Plan Review'),
      ),
      bottomNavigationBar: PastelBottomNavBar.roleBased(
        context: context,
        session: session,
        currentIndex: 1,
      ),
      body: _body(context, controller, isAdmin),
    );
  }

  Widget _body(BuildContext context, PlanController controller, bool isAdmin) {
    if (controller.loading && controller.plan == null) {
      return const Center(
        child: CircularProgressIndicator(
          valueColor: AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
        ),
      );
    }
    if (controller.plan == null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(AppDimens.space24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.error_outline_rounded,
                size: 44,
                color: AppColors.error,
              ),
              const SizedBox(height: 12),
              Text(
                controller.error ?? 'Plan unavailable',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: AppColors.textPrimary,
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 16),
              ElevatedButton.icon(
                style: AppButtonStyles.primary(),
                onPressed: controller.load,
                icon: const Icon(Icons.refresh_rounded, size: 18),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }
    final plan = controller.plan!;

    return RefreshIndicator(
      color: AppColors.obsidianBlack,
      backgroundColor: AppColors.surfacePure,
      onRefresh: controller.load,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppDimens.space20,
          AppDimens.space12,
          AppDimens.space20,
          AppDimens.space32,
        ),
        children: [
          if (controller.fromCache) _cacheBanner(),

          // 1. Highlights & Readiness Card (AppColors.pastelYellowLight)
          _buildHighlightsCard(context, plan),

          const SizedBox(height: AppDimens.space16),

          // 2. AI Recommendations & Rationale (AppColors.pastelBlueLight)
          _buildSectionCard(
            title: 'AI Recommendations & Rationale',
            icon: Icons.auto_awesome_rounded,
            bgColor: AppColors.pastelBlueLight,
            textColor: AppColors.pastelBlueText,
            borderColor: const Color(0x30BCD8F0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  plan.rationale.isEmpty
                      ? 'No AI rationale provided for this plan.'
                      : plan.rationale,
                  style: const TextStyle(
                    color: AppColors.textPrimary,
                    fontSize: 14,
                    height: 1.5,
                  ),
                ),
                if (plan.validationSummary.isNotEmpty) ...[
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: AppColors.surfacePure.withValues(alpha: 0.8),
                      borderRadius:
                          BorderRadius.circular(AppDimens.radiusMedium),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Icon(
                          Icons.verified_outlined,
                          size: 18,
                          color: AppColors.pastelBlueText,
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            plan.validationSummary,
                            style: const TextStyle(
                              color: AppColors.textSecondary,
                              fontSize: 13,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ],
            ),
          ),

          const SizedBox(height: AppDimens.space16),

          // 3. Timeline (AppColors.pastelLavenderLight)
          ProposedTimelineSection(
            timeline: plan.proposedTimeline,
          ),

          const SizedBox(height: AppDimens.space16),

          // 4. Tasks & Missing Requirements (AppColors.pastelGreenLight)
          _buildSectionCard(
            title:
                'Action Items & Requirements (${plan.missingRequirements.length})',
            icon: Icons.task_alt_rounded,
            bgColor: AppColors.pastelGreenLight,
            textColor: AppColors.pastelGreenText,
            borderColor: const Color(0x30C4DDB8),
            child: plan.missingRequirements.isEmpty
                ? const Row(
                    children: [
                      Icon(Icons.check_circle_rounded,
                          color: AppColors.success, size: 20),
                      SizedBox(width: 8),
                      Text(
                        'All requirements are satisfied!',
                        style: TextStyle(
                          color: AppColors.textPrimary,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  )
                : Column(
                    children: plan.missingRequirements.map((item) {
                      return Padding(
                        padding:
                            const EdgeInsets.only(bottom: AppDimens.space10),
                        child: Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: AppColors.surfacePure,
                            borderRadius:
                                BorderRadius.circular(AppDimens.radiusMedium),
                            border: Border.all(color: AppColors.borderSubtle),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  const Icon(
                                    Icons.radio_button_unchecked_rounded,
                                    size: 16,
                                    color: AppColors.pastelGreenText,
                                  ),
                                  const SizedBox(width: 8),
                                  Expanded(
                                    child: Text(
                                      item.requirement,
                                      style: const TextStyle(
                                        color: AppColors.textPrimary,
                                        fontWeight: FontWeight.w700,
                                        fontSize: 14,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                              if (item.reason.isNotEmpty) ...[
                                const SizedBox(height: 6),
                                Padding(
                                  padding: const EdgeInsets.only(left: 24),
                                  child: Text(
                                    item.reason,
                                    style: const TextStyle(
                                      color: AppColors.textSecondary,
                                      fontSize: 12,
                                    ),
                                  ),
                                ),
                              ],
                            ],
                          ),
                        ),
                      );
                    }).toList(),
                  ),
          ),

          const SizedBox(height: AppDimens.space16),

          // 5. Budget Allocation (AppColors.pastelPeachLight)
          _buildSectionCard(
            title: 'Budget Allocation',
            icon: Icons.pie_chart_rounded,
            bgColor: AppColors.pastelPeachLight,
            textColor: AppColors.pastelPeachText,
            borderColor: const Color(0x30FFD1B3),
            child: _buildBudgetBreakdown(context, plan),
          ),

          const SizedBox(height: AppDimens.space16),

          // 6. Vendor Suggestions & Categories (AppColors.pastelPinkLight)
          _buildSectionCard(
            title: 'Recommended Vendor Services',
            icon: Icons.storefront_rounded,
            bgColor: AppColors.pastelPinkLight,
            textColor: AppColors.pastelPinkText,
            borderColor: const Color(0x30F9BFD8),
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              children: plan.serviceCategories.map((item) {
                return Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                  decoration: BoxDecoration(
                    color: AppColors.surfacePure,
                    borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                    border: Border.all(
                      color: AppColors.pastelPink.withValues(alpha: 0.6),
                    ),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(
                        Icons.check_rounded,
                        size: 14,
                        color: AppColors.pastelPinkText,
                      ),
                      const SizedBox(width: 6),
                      Text(
                        item,
                        style: const TextStyle(
                          color: AppColors.pastelPinkText,
                          fontWeight: FontWeight.w700,
                          fontSize: 13,
                        ),
                      ),
                    ],
                  ),
                );
              }).toList(),
            ),
          ),

          const SizedBox(height: AppDimens.space16),

          // 7. Identified Risks
          if (plan.risks.isNotEmpty)
            _buildSectionCard(
              title: 'Identified Risks (${plan.risks.length})',
              icon: Icons.warning_amber_rounded,
              bgColor: AppColors.surfaceMuted,
              textColor: AppColors.textPrimary,
              borderColor: AppColors.borderSubtle,
              child: Column(
                children: plan.risks.map((risk) {
                  final color = _severityColor(risk.severity);
                  return Padding(
                    padding: const EdgeInsets.only(bottom: AppDimens.space10),
                    child: Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: AppColors.surfacePure,
                        borderRadius:
                            BorderRadius.circular(AppDimens.radiusMedium),
                        border: Border.all(color: AppColors.borderSubtle),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Icon(Icons.shield_outlined,
                                  size: 16, color: color),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Text(
                                  risk.risk,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w700,
                                    fontSize: 14,
                                    color: AppColors.textPrimary,
                                  ),
                                ),
                              ),
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 8,
                                  vertical: 3,
                                ),
                                decoration: BoxDecoration(
                                  color: color.withValues(alpha: 0.15),
                                  borderRadius: BorderRadius.circular(6),
                                ),
                                child: Text(
                                  risk.severity.toUpperCase(),
                                  style: TextStyle(
                                    color: color,
                                    fontSize: 11,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                              ),
                            ],
                          ),
                          if (risk.recommendation.isNotEmpty) ...[
                            const SizedBox(height: 6),
                            Text(
                              risk.recommendation,
                              style: const TextStyle(
                                color: AppColors.textSecondary,
                                fontSize: 13,
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
                  );
                }).toList(),
              ),
            ),

          if (controller.error != null)
            Padding(
              padding: const EdgeInsets.only(top: 14),
              child: Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppColors.errorBg,
                  borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
                ),
                child: Text(
                  controller.error!,
                  style: const TextStyle(
                    color: AppColors.error,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ),

          const SizedBox(height: AppDimens.space24),

          // Action Buttons: Approve / Reject / Regenerate
          if (!isAdmin && plan.status == PlanStatus.pendingPlannerReview)
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    style: AppButtonStyles.outline(),
                    onPressed: controller.submitting
                        ? null
                        : () => _reject(context, controller),
                    icon: const Icon(Icons.close_rounded, size: 18),
                    label: const Text('Reject Plan'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: ElevatedButton.icon(
                    style: AppButtonStyles.success(),
                    onPressed: controller.submitting
                        ? null
                        : () => _approve(context, controller),
                    icon: controller.submitting
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              valueColor: AlwaysStoppedAnimation<Color>(
                                  AppColors.pastelGreenText),
                            ),
                          )
                        : const Icon(Icons.check_circle_outline_rounded,
                            size: 18),
                    label: const Text('Approve Plan'),
                  ),
                ),
              ],
            ),

          if (!isAdmin && plan.status == PlanStatus.rejected)
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                style: AppButtonStyles.ai(),
                onPressed: controller.submitting
                    ? null
                    : () => _regenerate(context, controller),
                icon: const Icon(Icons.refresh_rounded),
                label: const Text('Regenerate Plan'),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildHighlightsCard(BuildContext context, EventPlan plan) {
    final score = (plan.completenessScore / 100).clamp(0.0, 1.0);

    return Container(
      padding: const EdgeInsets.all(AppDimens.space20),
      decoration: BoxDecoration(
        color: AppColors.pastelYellowLight,
        borderRadius: BorderRadius.circular(AppDimens.radiusCard),
        border: Border.all(color: const Color(0x40FEE388), width: 1.5),
        boxShadow: AppDimens.cardShadow,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  plan.event.eventName,
                  style: const TextStyle(
                    color: AppColors.textPrimary,
                    fontSize: 22,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.5,
                  ),
                ),
              ),
              PastelPillBadge(
                text: _statusLabel(plan.status).toUpperCase(),
                style: plan.status == PlanStatus.approved
                    ? PastelBadgeStyle.green
                    : plan.status == PlanStatus.rejected
                        ? PastelBadgeStyle.neutral
                        : PastelBadgeStyle.yellow,
              ),
            ],
          ),
          const SizedBox(height: 4),
          Text(
            '${_date(plan.event.eventDate)} · ${plan.event.location}',
            style: const TextStyle(
              color: AppColors.textSecondary,
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                '${plan.completenessScore}% plan completeness',
                style: const TextStyle(
                  color: AppColors.pastelYellowText,
                  fontWeight: FontWeight.w800,
                  fontSize: 15,
                ),
              ),
              Text(
                'Version ${plan.version}',
                style: const TextStyle(
                  color: AppColors.textMuted,
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          ClipRRect(
            borderRadius: BorderRadius.circular(6),
            child: LinearProgressIndicator(
              value: score,
              backgroundColor: AppColors.surfacePure,
              valueColor: const AlwaysStoppedAnimation<Color>(
                Color(0xFFE4BC32),
              ),
              minHeight: 10,
            ),
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              _buildTag(
                Icons.people_outline_rounded,
                '${plan.event.guestCount} guests',
              ),
              const SizedBox(width: 8),
              _buildTag(
                Icons.attach_money_rounded,
                'Budget ${_money(plan.event.budget)}',
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildSectionCard({
    required String title,
    required IconData icon,
    required Color bgColor,
    required Color textColor,
    required Color borderColor,
    required Widget child,
  }) {
    return Container(
      padding: const EdgeInsets.all(AppDimens.space20),
      decoration: BoxDecoration(
        color: bgColor,
        borderRadius: BorderRadius.circular(AppDimens.radiusCard),
        border: Border.all(color: borderColor, width: 1.2),
        boxShadow: AppDimens.cardShadow,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, color: textColor, size: 20),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  title,
                  style: TextStyle(
                    color: textColor,
                    fontSize: 16,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.3,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: AppDimens.space14),
          child,
        ],
      ),
    );
  }

  Widget _buildBudgetBreakdown(BuildContext context, EventPlan plan) {
    final allocated = plan.budgetAllocation.values
        .fold<double>(0, (sum, item) => sum + item);
    final ratio = plan.event.budget == 0
        ? 0.0
        : (allocated / plan.event.budget).clamp(0.0, 1.0);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              '${_money(allocated)} allocated',
              style: const TextStyle(
                color: AppColors.textPrimary,
                fontWeight: FontWeight.w700,
                fontSize: 14,
              ),
            ),
            Text(
              'Total ${_money(plan.event.budget)}',
              style: const TextStyle(
                color: AppColors.textSecondary,
                fontSize: 13,
              ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        ClipRRect(
          borderRadius: BorderRadius.circular(6),
          child: LinearProgressIndicator(
            value: ratio,
            backgroundColor: AppColors.surfacePure,
            valueColor: const AlwaysStoppedAnimation<Color>(
              Color(0xFFE8955A),
            ),
            minHeight: 8,
          ),
        ),
        const SizedBox(height: 14),
        ...plan.budgetAllocation.entries.map((entry) {
          return Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              decoration: BoxDecoration(
                color: AppColors.surfacePure,
                borderRadius: BorderRadius.circular(AppDimens.radiusSmall),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    entry.key,
                    style: const TextStyle(
                      fontWeight: FontWeight.w600,
                      fontSize: 13,
                    ),
                  ),
                  Text(
                    _money(entry.value),
                    style: const TextStyle(
                      fontWeight: FontWeight.w800,
                      fontSize: 13,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ],
              ),
            ),
          );
        }),
      ],
    );
  }

  Widget _buildTag(IconData icon, String text) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: AppColors.surfacePure,
        borderRadius: BorderRadius.circular(AppDimens.radiusSmall),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: AppColors.textSecondary),
          const SizedBox(width: 4),
          Text(
            text,
            style: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
            ),
          ),
        ],
      ),
    );
  }

  Widget _cacheBanner() => Container(
        margin: const EdgeInsets.only(bottom: 14),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: AppColors.surfaceMuted,
          borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
        ),
        child: const Row(
          children: [
            Icon(Icons.cloud_off, size: 18, color: AppColors.textSecondary),
            SizedBox(width: 8),
            Expanded(
              child: Text(
                'Showing the last saved copy. Connect to refresh or submit decisions.',
                style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
              ),
            ),
          ],
        ),
      );

  Future<void> _approve(
      BuildContext context, PlanController controller) async {
    final notes = await _notesDialog(
        context, 'Approve plan', 'Optional approver notes');
    if (!mounted || notes == null) return;
    if (await controller.approve(notes)) _message('Plan approved.');
  }

  Future<void> _reject(BuildContext context, PlanController controller) async {
    final result = await showDialog<(String, RejectionSeverity)>(
      context: context,
      builder: (_) => const _RejectDialog(),
    );
    if (!mounted || result == null) return;
    if (result.$1.trim().length < 20) return;
    if (await controller.reject(result.$1.trim(), result.$2)) {
      _message('Plan rejected.');
    }
  }

  Future<void> _regenerate(
      BuildContext context, PlanController controller) async {
    final reasonController = TextEditingController();
    final reason = await showDialog<String>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Regenerate plan'),
        content: TextField(
          controller: reasonController,
          autofocus: true,
          maxLength: 500,
          decoration: const InputDecoration(
            labelText: 'Reason for regeneration',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            style: AppButtonStyles.ai(),
            onPressed: () {
              final value = reasonController.text.trim();
              if (value.isNotEmpty) Navigator.pop(dialogContext, value);
            },
            child: const Text('Regenerate'),
          ),
        ],
      ),
    );
    reasonController.dispose();
    if (!mounted || reason == null) return;
    if (!await controller.regenerate(reason)) return;
    if (!context.mounted) return;
    final newPlanId = controller.plan?.id;
    if (newPlanId != null && newPlanId.isNotEmpty) {
      Navigator.pushReplacementNamed(
        context,
        '/plans/review',
        arguments: newPlanId,
      );
    }
  }

  Future<String?> _notesDialog(
      BuildContext context, String title, String hint) async {
    final input = TextEditingController();
    return showDialog<String>(
      context: context,
      builder: (_) => AlertDialog(
        title: Text(title),
        content: TextField(
          controller: input,
          maxLines: 4,
          decoration: InputDecoration(hintText: hint),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            style: AppButtonStyles.success(),
            onPressed: () => Navigator.pop(context, input.text),
            child: const Text('Yes, approve'),
          ),
        ],
      ),
    );
  }

  void _message(String message) => ScaffoldMessenger.of(context)
      .showSnackBar(SnackBar(content: Text(message)));

  String _date(DateTime date) => '${date.day}/${date.month}/${date.year}';
  String _money(double value) => '\$${value.toStringAsFixed(2)}';

  String _statusLabel(PlanStatus status) => switch (status) {
        PlanStatus.pendingPlannerReview => 'Pending Review',
        PlanStatus.approved => 'Approved',
        PlanStatus.rejected => 'Rejected',
        PlanStatus.superseded => 'Superseded',
        PlanStatus.draft => 'Draft',
      };

  Color _severityColor(String severity) =>
      severity.toLowerCase().contains('critical')
          ? AppColors.error
          : severity.toLowerCase().contains('high')
              ? AppColors.warning
              : AppColors.oliveRibbon;
}

class _RejectDialog extends StatefulWidget {
  const _RejectDialog();
  @override
  State<_RejectDialog> createState() => _RejectDialogState();
}

class _RejectDialogState extends State<_RejectDialog> {
  final remarks = TextEditingController();
  RejectionSeverity severity = RejectionSeverity.moderate;

  @override
  void initState() {
    super.initState();
    remarks.addListener(_remarksChanged);
  }

  @override
  void dispose() {
    remarks.removeListener(_remarksChanged);
    remarks.dispose();
    super.dispose();
  }

  void _remarksChanged() => setState(() {});

  @override
  Widget build(BuildContext context) => AlertDialog(
        title: const Text('Reject plan'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: remarks,
              minLines: 4,
              maxLines: 7,
              maxLength: 1000,
              decoration: const InputDecoration(
                labelText: 'Remarks (minimum 20 characters)',
              ),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<RejectionSeverity>(
              initialValue: severity,
              decoration: const InputDecoration(labelText: 'Severity'),
              items: RejectionSeverity.values
                  .map((item) => DropdownMenuItem(
                        value: item,
                        child: Text(item.name[0].toUpperCase() +
                            item.name.substring(1)),
                      ))
                  .toList(),
              onChanged: (value) =>
                  setState(() => severity = value ?? severity),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            style: AppButtonStyles.warning(),
            onPressed: remarks.text.trim().length < 20
                ? null
                : () => Navigator.pop(context, (remarks.text, severity)),
            child: const Text('Reject plan'),
          ),
        ],
      );
}
