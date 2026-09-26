import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/theme/app_colors.dart';
import '../../auth/providers/auth_providers.dart';
import '../models/plan_model.dart';
import '../providers/plan_providers.dart';

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
        ref.read(planProvider((token: session.accessToken, planId: widget.planId))).load();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(currentUserProvider);
    if (session == null) {
      return const Scaffold(body: Center(child: Text('Please sign in to review plans.')));
    }
    final controller = ref.watch(
        planProvider((token: session.accessToken, planId: widget.planId)));
    return Scaffold(
      appBar: AppBar(title: const Text('Plan review')),
      body: _body(context, controller),
    );
  }

  Widget _body(BuildContext context, PlanController controller) {
    if (controller.loading && controller.plan == null) {
      return const Center(child: CircularProgressIndicator());
    }
    if (controller.plan == null) {
      return Center(
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          Text(controller.error ?? 'Plan unavailable'),
          const SizedBox(height: 12),
          OutlinedButton(onPressed: controller.load, child: const Text('Retry')),
        ]),
      );
    }
    final plan = controller.plan!;
    return RefreshIndicator(
      onRefresh: controller.load,
      child: ListView(padding: const EdgeInsets.all(20), children: [
        if (controller.fromCache) _cacheBanner(),
        Text(plan.event.eventName,
            style: Theme.of(context).textTheme.headlineMedium),
        const SizedBox(height: 6),
        Text('${_date(plan.event.eventDate)} · ${plan.event.location}'),
        const SizedBox(height: 18),
        _summaryCard(context, plan),
        _section('Service categories', Wrap(
          spacing: 8,
          runSpacing: 8,
          children: plan.serviceCategories.map((item) => Chip(label: Text(item))).toList(),
        )),
        _section('Budget overview', _budgetOverview(context, plan)),
        _section('Timeline', Column(
          children: plan.proposedTimeline.entries
              .map((entry) => ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: const Icon(Icons.schedule),
                    title: Text(entry.key),
                    subtitle: Text(entry.value),
                  ))
              .toList(),
        )),
        _collapsible('Identified risks (${plan.risks.length})',
            plan.risks.map((risk) => ListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(risk.risk),
              subtitle: Text(risk.recommendation),
              leading: Icon(Icons.warning_amber_rounded, color: _severityColor(risk.severity)),
              onTap: () => _showRisk(context, risk),
            )).toList()),
        _collapsible('Missing requirements (${plan.missingRequirements.length})',
            plan.missingRequirements.map((item) => ListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(item.requirement),
              subtitle: Text(item.reason),
            )).toList()),
        _section('Rationale', Text(plan.rationale.isEmpty ? 'No rationale provided.' : plan.rationale)),
        if (controller.error != null) Padding(
          padding: const EdgeInsets.only(top: 12),
          child: Text(controller.error!, style: const TextStyle(color: AppColors.error)),
        ),
        const SizedBox(height: 18),
        if (plan.status == PlanStatus.pendingPlannerReview)
          Row(children: [
            Expanded(child: OutlinedButton(
              onPressed: controller.submitting ? null : () => _reject(context, controller),
              child: const Text('Reject'),
            )),
            const SizedBox(width: 12),
            Expanded(child: FilledButton(
              onPressed: controller.submitting ? null : () => _approve(context, controller),
              child: controller.submitting
                  ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Approve'),
            )),
          ]),
        if (plan.status == PlanStatus.rejected)
          FilledButton.icon(
            onPressed: controller.submitting ? null : controller.regenerate,
            icon: const Icon(Icons.refresh),
            label: const Text('Regenerate plan'),
          ),
      ]),
    );
  }

  Widget _summaryCard(BuildContext context, EventPlan plan) {
    final score = (plan.completenessScore / 100).clamp(0.0, 1.0);
    final color = score < .5 ? AppColors.error : score < .75 ? AppColors.warning : AppColors.success;
    return Card(child: Padding(padding: const EdgeInsets.all(16), child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
          Text('${plan.completenessScore}% complete', style: Theme.of(context).textTheme.titleMedium),
          Chip(label: Text(_statusLabel(plan.status))),
        ]),
        const SizedBox(height: 10),
        LinearProgressIndicator(value: score, color: color, minHeight: 8),
        const SizedBox(height: 12),
        Text('Version ${plan.version} · ${plan.event.guestCount} guests · Budget ${_money(plan.event.budget)}'),
      ],
    )));
  }

  Widget _budgetOverview(BuildContext context, EventPlan plan) {
    final allocated = plan.budgetAllocation.values.fold<double>(0, (sum, item) => sum + item);
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text('${_money(allocated)} allocated of ${_money(plan.event.budget)}'),
      const SizedBox(height: 8),
      LinearProgressIndicator(value: plan.event.budget == 0 ? 0 : (allocated / plan.event.budget).clamp(0, 1)),
      ...plan.budgetAllocation.entries.map((entry) =>
          ListTile(contentPadding: EdgeInsets.zero, title: Text(entry.key), trailing: Text(_money(entry.value)))),
    ]);
  }

  Widget _section(String title, Widget child) => Padding(
      padding: const EdgeInsets.only(top: 22),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(title, style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w800)),
        const SizedBox(height: 10),
        child,
      ]));

  Widget _collapsible(String title, List<Widget> children) => Padding(
        padding: const EdgeInsets.only(top: 12),
        child: Card(child: ExpansionTile(title: Text(title), children: children)),
      );

  Widget _cacheBanner() => const Padding(
      padding: EdgeInsets.only(bottom: 12),
      child: Row(children: [
        Icon(Icons.cloud_off, size: 18),
        SizedBox(width: 8),
        Expanded(child: Text('Showing the last saved copy. Connect to refresh or submit decisions.')),
      ]));

  Future<void> _approve(BuildContext context, PlanController controller) async {
    final notes = await _notesDialog(context, 'Approve plan', 'Optional approver notes');
    if (!mounted || notes == null) return;
    if (await controller.approve(notes)) _message('Plan approved.');
  }

  Future<void> _reject(BuildContext context, PlanController controller) async {
    final result = await showDialog<(String, RejectionSeverity)>(
        context: context, builder: (_) => const _RejectDialog());
    if (!mounted || result == null) return;
    if (result.$1.trim().length < 20) return;
    if (await controller.reject(result.$1.trim(), result.$2)) _message('Plan rejected.');
  }

  Future<String?> _notesDialog(BuildContext context, String title, String hint) async {
    final input = TextEditingController();
    return showDialog<String>(context: context, builder: (_) => AlertDialog(
      title: Text(title),
      content: TextField(controller: input, maxLines: 4, decoration: InputDecoration(hintText: hint)),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
        FilledButton(onPressed: () => Navigator.pop(context, input.text), child: const Text('Yes, approve')),
      ],
    ));
  }

  void _showRisk(BuildContext context, PlanRisk risk) => showModalBottomSheet(
      context: context,
      showDragHandle: true,
      builder: (_) => Padding(padding: const EdgeInsets.all(24), child: Column(
        mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start,
        children: [Text(risk.risk, style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 10), Text('Severity: ${risk.severity}'),
          const SizedBox(height: 10), Text(risk.recommendation), const SizedBox(height: 18)],
      )));

  void _message(String message) => ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  String _date(DateTime date) => '${date.day}/${date.month}/${date.year}';
  String _money(double value) => '\$${value.toStringAsFixed(2)}';
  String _statusLabel(PlanStatus status) => switch (status) {
        PlanStatus.pendingPlannerReview => 'Pending',
        PlanStatus.approved => 'Approved',
        PlanStatus.rejected => 'Rejected',
        PlanStatus.superseded => 'Superseded',
        PlanStatus.draft => 'Draft',
      };
  Color _severityColor(String severity) => severity.toLowerCase().contains('critical')
      ? AppColors.error : severity.toLowerCase().contains('high') ? AppColors.warning : AppColors.oliveRibbon;
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
    content: Column(mainAxisSize: MainAxisSize.min, children: [
      TextField(controller: remarks, minLines: 4, maxLines: 7, maxLength: 1000,
          decoration: const InputDecoration(labelText: 'Remarks (minimum 20 characters)')),
      DropdownButtonFormField<RejectionSeverity>(
        initialValue: severity,
        decoration: const InputDecoration(labelText: 'Severity'),
        items: RejectionSeverity.values.map((item) => DropdownMenuItem(
            value: item, child: Text(item.name[0].toUpperCase() + item.name.substring(1)))).toList(),
        onChanged: (value) => setState(() => severity = value ?? severity),
      ),
    ]),
    actions: [
      TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
      FilledButton(onPressed: remarks.text.trim().length < 20 ? null : () =>
          Navigator.pop(context, (remarks.text, severity)), child: const Text('Reject plan')),
    ],
  );
}
