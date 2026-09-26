import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/quotation_remote_datasource.dart';
import '../models/quotation_model.dart';

class VendorQuotationDetailPage extends StatefulWidget {
  const VendorQuotationDetailPage({super.key});

  @override
  State<VendorQuotationDetailPage> createState() =>
      _VendorQuotationDetailPageState();
}

class _VendorQuotationDetailPageState extends State<VendorQuotationDetailPage> {
  final _api = QuotationRemoteDataSource();
  final _priceController = TextEditingController();
  final _termsController = TextEditingController();

  AuthResponseModel? _auth;
  QuotationModel? _quotation;
  bool _loading = true;
  bool _submitting = false;
  String? _error;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_loading) {
      _load();
    }
  }

  @override
  void dispose() {
    _priceController.dispose();
    _termsController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final args = ModalRoute.of(context)?.settings.arguments;
    if (args is! Map) {
      setState(() {
        _loading = false;
        _error = 'Missing quotation details.';
      });
      return;
    }

    final auth = args['auth'];
    final quotationId = args['quotationId']?.toString();
    if (auth is! AuthResponseModel ||
        quotationId == null ||
        quotationId.isEmpty) {
      setState(() {
        _loading = false;
        _error = 'Missing quotation details.';
      });
      return;
    }

    try {
      final quotation = await _api.getById(auth.accessToken, quotationId);
      if (!mounted) return;
      setState(() {
        _auth = auth;
        _quotation = quotation;
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

  Future<void> _respond() async {
    final auth = _auth;
    final quotation = _quotation;
    if (auth == null || quotation == null) return;

    final price = double.tryParse(_priceController.text.trim());
    if (price == null || price < 0) {
      setState(() => _error = 'Enter a valid non-negative quoted price.');
      return;
    }

    setState(() {
      _submitting = true;
      _error = null;
    });

    try {
      final updated = await _api.respond(
        auth.accessToken,
        quotation.id,
        quotedPrice: price,
        vendorTerms: _termsController.text.trim().isEmpty
            ? null
            : _termsController.text.trim(),
      );
      if (!mounted) return;
      setState(() {
        _quotation = updated;
        _submitting = false;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Quotation response submitted.')),
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
    final q = _quotation;

    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(title: const Text('Quotation details')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && q == null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Text(_error!, style: const TextStyle(color: AppColors.error)),
                  ),
                )
              : q == null
                  ? const SizedBox.shrink()
                  : ListView(
                      padding: const EdgeInsets.all(AppDimens.space20),
                      children: [
                        PastelSectionHeader(title: q.serviceName),
                        PastelCard(
                          padding: const EdgeInsets.all(AppDimens.space16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('Status: ${q.displayStatus}'),
                              Text('Event type: ${q.eventType ?? '—'}'),
                              Text('Guests: ${q.guestCount ?? '—'}'),
                              Text(
                                'Preferred date: ${q.eventPreferredDate?.toLocal() ?? '—'}',
                              ),
                              Text(
                                'Event requirements: ${q.eventRequirements ?? '—'}',
                              ),
                              Text(q.displayRange),
                              Text('Customer message: ${q.customerMessage ?? '—'}'),
                              if (q.status == 'QUOTED') ...[
                                Text('Quoted price: ${q.displayQuotedPrice}'),
                                Text('Terms: ${q.vendorTerms ?? '—'}'),
                              ],
                            ],
                          ),
                        ),
                        if (q.status == 'REQUESTED') ...[
                          const PastelSectionHeader(title: 'Respond'),
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
                              children: [
                                TextField(
                                  controller: _priceController,
                                  keyboardType: const TextInputType.numberWithOptions(
                                    decimal: true,
                                  ),
                                  decoration: const InputDecoration(
                                    labelText: 'Quoted price (Rs.)',
                                  ),
                                ),
                                const SizedBox(height: 12),
                                TextField(
                                  controller: _termsController,
                                  maxLines: 3,
                                  decoration: const InputDecoration(
                                    labelText: 'Terms / notes (optional)',
                                  ),
                                ),
                                const SizedBox(height: 16),
                                ElevatedButton(
                                  onPressed: _submitting ? null : _respond,
                                  child: Text(
                                    _submitting ? 'Submitting…' : 'Submit response',
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ],
                    ),
    );
  }
}
