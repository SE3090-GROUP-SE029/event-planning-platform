import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/vendor_remote_datasource.dart';
import '../models/vendor_availability_model.dart';

class VendorAvailabilityPage extends StatefulWidget {
  const VendorAvailabilityPage({super.key});

  @override
  State<VendorAvailabilityPage> createState() => _VendorAvailabilityPageState();
}

class _VendorAvailabilityPageState extends State<VendorAvailabilityPage> {
  final _formKey = GlobalKey<FormState>();
  final _vendorApi = VendorRemoteDataSource();

  List<VendorAvailabilityModel> _periods = [];
  String? _editingId;
  DateTime? _startLocal;
  DateTime? _endLocal;
  bool _isAvailable = true;
  bool _loading = true;
  bool _saving = false;
  String? _error;

  AuthResponseModel? get _auth =>
      ModalRoute.of(context)?.settings.arguments as AuthResponseModel?;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_loading) {
      _loadPeriods();
    }
  }

  Future<void> _loadPeriods() async {
    final token = _auth?.accessToken;
    if (token == null || token.isEmpty) {
      setState(() {
        _loading = false;
        _error = 'Please log in as a vendor first.';
      });
      return;
    }

    try {
      final periods = await _vendorApi.listMyAvailability(token);
      if (!mounted) return;
      setState(() {
        _periods = periods;
        _loading = false;
        _error = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = e.toString();
      });
    }
  }

  void _startEdit(VendorAvailabilityModel period) {
    setState(() {
      _editingId = period.id;
      _startLocal = period.startDateTime.toLocal();
      _endLocal = period.endDateTime.toLocal();
      _isAvailable = period.isAvailable;
    });
  }

  void _clearForm() {
    setState(() {
      _editingId = null;
      _startLocal = null;
      _endLocal = null;
      _isAvailable = true;
    });
  }

  Future<void> _pickDateTime({required bool isStart}) async {
    final now = DateTime.now();
    final initial = (isStart ? _startLocal : _endLocal) ?? now;
    final date = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: DateTime(now.year - 1),
      lastDate: DateTime(now.year + 5),
    );
    if (date == null || !mounted) return;

    final time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(initial),
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
      if (isStart) {
        _startLocal = selected;
      } else {
        _endLocal = selected;
      }
    });
  }

  String _formatLocal(DateTime? value) {
    if (value == null) return 'Select date/time';
    final y = value.year.toString().padLeft(4, '0');
    final m = value.month.toString().padLeft(2, '0');
    final d = value.day.toString().padLeft(2, '0');
    final h = value.hour.toString().padLeft(2, '0');
    final min = value.minute.toString().padLeft(2, '0');
    return '$y-$m-$d $h:$min';
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    final token = _auth?.accessToken;
    if (token == null) return;

    if (_startLocal == null || _endLocal == null) {
      setState(() => _error = 'Start and end date/times are required.');
      return;
    }
    if (!_startLocal!.isBefore(_endLocal!)) {
      setState(() => _error = 'Start date/time must be before end date/time.');
      return;
    }

    final payload = VendorAvailabilityModel(
      id: _editingId ?? '',
      vendorId: '',
      startDateTime: _startLocal!.toUtc(),
      endDateTime: _endLocal!.toUtc(),
      isAvailable: _isAvailable,
    );

    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      if (_editingId == null) {
        await _vendorApi.createAvailability(token, payload);
      } else {
        await _vendorApi.updateAvailability(token, _editingId!, payload);
      }

      if (!mounted) return;
      _clearForm();
      final periods = await _vendorApi.listMyAvailability(token);
      if (!mounted) return;
      setState(() {
        _periods = periods;
        _saving = false;
      });

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Availability saved.'),
          backgroundColor: AppColors.success,
        ),
      );
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _error = e.toString();
      });
    }
  }

  Future<void> _delete(String id) async {
    final token = _auth?.accessToken;
    if (token == null) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete availability?'),
        content: const Text('This removes the period from your calendar.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;
    if (!mounted) return;

    try {
      await _vendorApi.deleteAvailability(token, id);
      if (!mounted) return;
      final periods = await _vendorApi.listMyAvailability(token);
      if (!mounted) return;
      setState(() {
        _periods = periods;
        if (_editingId == id) {
          _clearForm();
        }
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _error = e.toString());
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('Vendor availability'),
      ),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(
                valueColor:
                    AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
              ),
            )
          : SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(
                AppDimens.space20,
                AppDimens.space12,
                AppDimens.space20,
                AppDimens.space32,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (_error != null) ...[
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(AppDimens.space14),
                      decoration: BoxDecoration(
                        color: AppColors.errorBg,
                        borderRadius:
                            BorderRadius.circular(AppDimens.radiusMedium),
                      ),
                      child: Text(
                        _error!,
                        style: const TextStyle(
                          color: AppColors.error,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                    const SizedBox(height: AppDimens.space16),
                  ],
                  PastelSectionHeader(
                    title: _editingId == null
                        ? 'Add availability'
                        : 'Edit availability',
                  ),
                  PastelCard(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Form(
                      key: _formKey,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          ListTile(
                            contentPadding: EdgeInsets.zero,
                            title: const Text('Start date/time'),
                            subtitle: Text(_formatLocal(_startLocal)),
                            trailing: const Icon(Icons.schedule_outlined),
                            onTap: () => _pickDateTime(isStart: true),
                          ),
                          ListTile(
                            contentPadding: EdgeInsets.zero,
                            title: const Text('End date/time'),
                            subtitle: Text(_formatLocal(_endLocal)),
                            trailing: const Icon(Icons.schedule_outlined),
                            onTap: () => _pickDateTime(isStart: false),
                          ),
                          const SizedBox(height: AppDimens.space8),
                          const Text(
                            'Status',
                            style: TextStyle(fontWeight: FontWeight.w700),
                          ),
                          RadioGroup<bool>(
                            groupValue: _isAvailable,
                            onChanged: (value) {
                              if (value != null) {
                                setState(() => _isAvailable = value);
                              }
                            },
                            child: const Column(
                              children: [
                                RadioListTile<bool>(
                                  contentPadding: EdgeInsets.zero,
                                  title: Text('Available'),
                                  value: true,
                                ),
                                RadioListTile<bool>(
                                  contentPadding: EdgeInsets.zero,
                                  title: Text('Unavailable'),
                                  value: false,
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: AppDimens.space16),
                          SizedBox(
                            width: double.infinity,
                            height: 48,
                            child: ElevatedButton(
                              onPressed: _saving ? null : _save,
                              child: Text(
                                _editingId == null
                                    ? 'Add period'
                                    : 'Save changes',
                              ),
                            ),
                          ),
                          if (_editingId != null) ...[
                            const SizedBox(height: AppDimens.space8),
                            TextButton(
                              onPressed: _clearForm,
                              child: const Text('Cancel edit'),
                            ),
                          ],
                        ],
                      ),
                    ),
                  ),
                  const PastelSectionHeader(title: 'Your availability'),
                  if (_periods.isEmpty)
                    const PastelCard(
                      padding: EdgeInsets.all(AppDimens.space20),
                      child: Text(
                        'No periods yet. Dates without an available period stay not bookable.',
                      ),
                    )
                  else
                    ..._periods.map(
                      (period) => Padding(
                        padding:
                            const EdgeInsets.only(bottom: AppDimens.space12),
                        child: PastelCard(
                          padding: const EdgeInsets.all(AppDimens.space16),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      period.isAvailable
                                          ? 'Available'
                                          : 'Unavailable',
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w800,
                                        fontSize: 16,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      period.displayRange,
                                      style: const TextStyle(
                                        color: AppColors.textSecondary,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              IconButton(
                                onPressed: () => _startEdit(period),
                                icon: const Icon(Icons.edit_outlined),
                              ),
                              IconButton(
                                onPressed: () => _delete(period.id),
                                icon: const Icon(Icons.delete_outline),
                                color: AppColors.error,
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                ],
              ),
            ),
    );
  }
}
