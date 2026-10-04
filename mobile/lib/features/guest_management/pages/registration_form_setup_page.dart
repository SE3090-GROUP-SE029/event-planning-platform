import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/registration_form_model.dart';
import '../providers/guest_management_providers.dart';

class RegistrationFormSetupPage extends ConsumerStatefulWidget {
  final String eventId;
  final String eventName;

  const RegistrationFormSetupPage({
    super.key,
    required this.eventId,
    required this.eventName,
  });

  @override
  ConsumerState<RegistrationFormSetupPage> createState() =>
      _RegistrationFormSetupPageState();
}

class _RegistrationFormSetupPageState
    extends ConsumerState<RegistrationFormSetupPage> {
  final _formKey = GlobalKey<FormState>();
  final _seatLimitController = TextEditingController();
  DateTime? _opensAt;
  DateTime? _closesAt;
  PlannerFormModel? _form;
  bool _loading = true;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadForm();
  }

  @override
  void dispose() {
    _seatLimitController.dispose();
    super.dispose();
  }

  Future<void> _loadForm() async {
    try {
      final form =
          await ref.read(guestManagementApiProvider).getForm(widget.eventId);
      if (!mounted) return;
      setState(() {
        _form = form;
        _opensAt = form.opensAt.toLocal();
        _closesAt = form.closesAt.toLocal();
        _seatLimitController.text = form.seatLimit.toString();
        _loading = false;
      });
    } on DioException catch (error) {
      if (!mounted) return;
      if (error.response?.statusCode == 404) {
        setState(() => _loading = false);
      } else {
        setState(() {
          _error = _errorMessage(error);
          _loading = false;
        });
      }
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error.toString();
        _loading = false;
      });
    }
  }

  Future<void> _selectDateTime({required bool opening}) async {
    final current = opening ? _opensAt : _closesAt;
    final now = DateTime.now();
    final firstYear = current == null
        ? now.year - 1
        : (current.year < now.year - 1 ? current.year : now.year - 1);
    final lastYear = current == null
        ? now.year + 10
        : (current.year > now.year + 10 ? current.year : now.year + 10);
    final date = await showDatePicker(
      context: context,
      initialDate: current ?? now,
      firstDate: DateTime(firstYear),
      lastDate: DateTime(lastYear, 12, 31),
    );
    if (date == null || !mounted) return;
    final time = await showTimePicker(
      context: context,
      initialTime: current == null
          ? TimeOfDay.fromDateTime(now)
          : TimeOfDay.fromDateTime(current),
    );
    if (time == null || !mounted) return;
    final selected = DateTime(
      date.year,
      date.month,
      date.day,
      time.hour,
      time.minute,
    );
    setState(() {
      if (opening) {
        _opensAt = selected;
      } else {
        _closesAt = selected;
      }
    });
  }

  Future<void> _saveAndPublish() async {
    if (!_formKey.currentState!.validate()) return;
    final opensAt = _opensAt;
    final closesAt = _closesAt;
    if (opensAt == null || closesAt == null || !opensAt.isBefore(closesAt)) {
      setState(() => _error = 'Choose an opening time before the closing time.');
      return;
    }

    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      final api = ref.read(guestManagementApiProvider);
      final savedForm = _form == null
          ? await api.createForm(
              widget.eventId,
              opensAt: opensAt,
              closesAt: closesAt,
              seatLimit: int.parse(_seatLimitController.text),
            )
          : await api.updateForm(
              widget.eventId,
              opensAt: opensAt,
              closesAt: closesAt,
              seatLimit: int.parse(_seatLimitController.text),
            );
      if (mounted) setState(() => _form = savedForm);
      final publishedForm = await api.publishForm(widget.eventId);
      if (mounted) setState(() => _form = publishedForm);
      ref.invalidate(plannerFormProvider(widget.eventId));
      if (mounted) Navigator.of(context).pop(true);
    } on DioException catch (error) {
      if (mounted) setState(() => _error = _errorMessage(error));
    } catch (error) {
      if (mounted) setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  String _errorMessage(DioException error) {
    final data = error.response?.data;
    if (data is Map<String, dynamic>) {
      final message = data['error'] ?? data['message'];
      if (message is String && message.isNotEmpty) return message;
    }
    return error.message ?? 'The registration form request failed.';
  }

  String _formatDateTime(DateTime? value) {
    if (value == null) return 'Select date and time';
    final local = value.toLocal();
    final date =
        '${local.year.toString().padLeft(4, '0')}-${local.month.toString().padLeft(2, '0')}-${local.day.toString().padLeft(2, '0')}';
    final time =
        '${local.hour.toString().padLeft(2, '0')}:${local.minute.toString().padLeft(2, '0')}';
    return '$date $time';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Registration form')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && _form == null
              ? _buildLoadError()
              : _buildForm(),
    );
  }

  Widget _buildLoadError() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            OutlinedButton(
              onPressed: () => Navigator.of(context).pop(),
              child: const Text('Back to guests'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildForm() {
    return SafeArea(
      child: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Text(
              widget.eventName,
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 8),
            const Text(
              'Set the registration window and capacity. The selected local times are sent to the server in UTC.',
            ),
            const SizedBox(height: 20),
            _dateTimeField(
              label: 'Registration opens',
              value: _opensAt,
              onTap: () => _selectDateTime(opening: true),
            ),
            const SizedBox(height: 12),
            _dateTimeField(
              label: 'Registration closes',
              value: _closesAt,
              onTap: () => _selectDateTime(opening: false),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _seatLimitController,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(
                labelText: 'Seat limit',
                border: OutlineInputBorder(),
              ),
              validator: (value) {
                final seats = int.tryParse(value ?? '');
                if (seats == null || seats < 1) {
                  return 'Enter a seat limit of at least 1.';
                }
                return null;
              },
            ),
            if (_error != null) ...[
              const SizedBox(height: 16),
              Text(
                _error!,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            ],
            const SizedBox(height: 24),
            FilledButton(
              onPressed: _saving ? null : _saveAndPublish,
              child: _saving
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : Text(_form?.status == 'PUBLISHED'
                      ? 'Save form settings'
                      : 'Save and publish'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _dateTimeField({
    required String label,
    required DateTime? value,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: _saving ? null : onTap,
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          border: const OutlineInputBorder(),
          suffixIcon: const Icon(Icons.calendar_month_outlined),
        ),
        child: Text(_formatDateTime(value)),
      ),
    );
  }
}
