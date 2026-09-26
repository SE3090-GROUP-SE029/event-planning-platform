import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../../events/models/event_model.dart';
import '../../vendors/api/vendor_remote_datasource.dart';
import '../../vendors/models/marketplace_vendor_model.dart';
import '../api/quotation_remote_datasource.dart';

class RequestQuotationPage extends StatefulWidget {
  const RequestQuotationPage({super.key});

  @override
  State<RequestQuotationPage> createState() => _RequestQuotationPageState();
}

class _RequestQuotationPageState extends State<RequestQuotationPage> {
  final _quotationApi = QuotationRemoteDataSource();
  final _vendorApi = VendorRemoteDataSource();
  final _messageController = TextEditingController();

  AuthResponseModel? _auth;
  String? _vendorId;
  MarketplaceVendorDetail? _vendor;
  List<EventModel> _events = [];
  String? _selectedServiceId;
  String? _selectedEventId;
  DateTime? _start;
  DateTime? _end;
  bool _loading = true;
  bool _submitting = false;
  String? _error;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_loading) {
      _bootstrap();
    }
  }

  @override
  void dispose() {
    _messageController.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    final args = ModalRoute.of(context)?.settings.arguments;
    if (args is! Map) {
      setState(() {
        _loading = false;
        _error = 'Missing quotation request details.';
      });
      return;
    }

    final auth = args['auth'];
    final vendorId = args['vendorId']?.toString();
    final preselectedService = args['serviceId']?.toString();

    if (auth is! AuthResponseModel ||
        vendorId == null ||
        vendorId.isEmpty ||
        auth.accessToken.isEmpty) {
      setState(() {
        _loading = false;
        _error = 'Missing quotation request details.';
      });
      return;
    }

    try {
      final vendor =
          await _vendorApi.getMarketplaceVendor(auth.accessToken, vendorId);
      final events = await _quotationApi.listMyEvents(auth.accessToken);
      if (!mounted) return;
      setState(() {
        _auth = auth;
        _vendorId = vendorId;
        _vendor = vendor;
        _events = events;
        _selectedServiceId = preselectedService ??
            (vendor.services.isNotEmpty ? vendor.services.first.id : null);
        _selectedEventId = events.isNotEmpty ? events.first.id : null;
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = e.toString();
      });
    }
  }

  Future<void> _pickDateTime({required bool isStart}) async {
    final now = DateTime.now();
    final date = await showDatePicker(
      context: context,
      initialDate: now,
      firstDate: now.subtract(const Duration(days: 1)),
      lastDate: now.add(const Duration(days: 365 * 2)),
    );
    if (date == null || !mounted) return;
    final time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(now),
    );
    if (time == null || !mounted) return;
    final value = DateTime(date.year, date.month, date.day, time.hour, time.minute);
    setState(() {
      if (isStart) {
        _start = value;
      } else {
        _end = value;
      }
    });
  }

  Future<void> _submit() async {
    final auth = _auth;
    final vendorId = _vendorId;
    if (auth == null || vendorId == null) return;

    if (_selectedServiceId == null ||
        _selectedEventId == null ||
        _start == null ||
        _end == null) {
      setState(() => _error = 'Service, event, start, and end are required.');
      return;
    }

    setState(() {
      _submitting = true;
      _error = null;
    });

    try {
      await _quotationApi.create(
        auth.accessToken,
        vendorId: vendorId,
        vendorServiceId: _selectedServiceId!,
        eventId: _selectedEventId!,
        requestedStartDateTime: _start!,
        requestedEndDateTime: _end!,
        customerMessage: _messageController.text.trim().isEmpty
            ? null
            : _messageController.text.trim(),
      );
      if (!mounted) return;
      Navigator.of(context).pushReplacementNamed(
        '/quotations/mine',
        arguments: auth,
      );
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _submitting = false;
        _error = e.toString();
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final vendor = _vendor;

    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(title: const Text('Request quotation')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && vendor == null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Text(_error!, style: const TextStyle(color: AppColors.error)),
                  ),
                )
              : ListView(
                  padding: const EdgeInsets.all(AppDimens.space20),
                  children: [
                    PastelSectionHeader(
                      title: vendor?.businessName ?? 'Vendor',
                    ),
                    if (_error != null)
                      PastelCard(
                        padding: const EdgeInsets.all(AppDimens.space16),
                        child: Text(
                          _error!,
                          style: const TextStyle(color: AppColors.error),
                        ),
                      ),
                    PastelCard(
                      padding: const EdgeInsets.all(AppDimens.space16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Service', style: TextStyle(fontWeight: FontWeight.w700)),
                          const SizedBox(height: 8),
                          DropdownButtonFormField<String>(
                            initialValue: _selectedServiceId,
                            items: (vendor?.services ?? [])
                                .map(
                                  (s) => DropdownMenuItem(
                                    value: s.id,
                                    child: Text(s.serviceName),
                                  ),
                                )
                                .toList(),
                            onChanged: (value) =>
                                setState(() => _selectedServiceId = value),
                          ),
                          const SizedBox(height: 16),
                          const Text('Your event', style: TextStyle(fontWeight: FontWeight.w700)),
                          const SizedBox(height: 8),
                          DropdownButtonFormField<String>(
                            initialValue: _selectedEventId,
                            items: _events
                                .map(
                                  (e) => DropdownMenuItem(
                                    value: e.id,
                                    child: Text(
                                      '${e.eventType.name.toUpperCase()} · ${e.guestCount} guests',
                                    ),
                                  ),
                                )
                                .toList(),
                            onChanged: (value) =>
                                setState(() => _selectedEventId = value),
                          ),
                          if (_events.isEmpty) ...[
                            const SizedBox(height: 8),
                            const Text(
                              'Create an event first before requesting a quotation.',
                              style: TextStyle(color: AppColors.textSecondary),
                            ),
                          ],
                          const SizedBox(height: 16),
                          ListTile(
                            contentPadding: EdgeInsets.zero,
                            title: const Text('Requested start'),
                            subtitle: Text(_start?.toString() ?? 'Select start'),
                            trailing: const Icon(Icons.schedule),
                            onTap: () => _pickDateTime(isStart: true),
                          ),
                          ListTile(
                            contentPadding: EdgeInsets.zero,
                            title: const Text('Requested end'),
                            subtitle: Text(_end?.toString() ?? 'Select end'),
                            trailing: const Icon(Icons.schedule),
                            onTap: () => _pickDateTime(isStart: false),
                          ),
                          TextField(
                            controller: _messageController,
                            maxLines: 3,
                            decoration: const InputDecoration(
                              labelText: 'Requirements / message (optional)',
                            ),
                          ),
                          const SizedBox(height: 16),
                          ElevatedButton(
                            onPressed: _submitting || _events.isEmpty ? null : _submit,
                            child: Text(_submitting ? 'Submitting…' : 'Submit request'),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
    );
  }
}
