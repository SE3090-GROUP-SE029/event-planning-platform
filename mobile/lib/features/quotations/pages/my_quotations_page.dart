import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/quotation_remote_datasource.dart';
import '../models/quotation_model.dart';

class MyQuotationsPage extends StatefulWidget {
  const MyQuotationsPage({super.key});

  @override
  State<MyQuotationsPage> createState() => _MyQuotationsPageState();
}

class _MyQuotationsPageState extends State<MyQuotationsPage> {
  final _api = QuotationRemoteDataSource();
  AuthResponseModel? _auth;
  List<QuotationModel> _items = [];
  bool _loading = true;
  String? _error;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_loading) {
      _load();
    }
  }

  Future<void> _load() async {
    final args = ModalRoute.of(context)?.settings.arguments;
    final auth = args is AuthResponseModel
        ? args
        : (args is Map ? args['auth'] : null);

    if (auth is! AuthResponseModel || auth.accessToken.isEmpty) {
      setState(() {
        _loading = false;
        _error = 'Missing session.';
      });
      return;
    }

    try {
      final items = await _api.listMine(auth.accessToken);
      if (!mounted) return;
      setState(() {
        _auth = auth;
        _items = items;
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(title: const Text('My quotations')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Text(_error!, style: const TextStyle(color: AppColors.error)),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    children: [
                      const PastelSectionHeader(
                        title: 'Your quotation requests',
                      ),
                      if (_items.isEmpty)
                        const PastelCard(
                          padding: EdgeInsets.all(AppDimens.space20),
                          child: Text('No quotations yet.'),
                        )
                      else
                        ..._items.map(
                          (q) => Padding(
                            padding: const EdgeInsets.only(bottom: AppDimens.space12),
                            child: PastelCard(
                              padding: const EdgeInsets.all(AppDimens.space16),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    q.vendorBusinessName,
                                    style: const TextStyle(fontWeight: FontWeight.w800),
                                  ),
                                  Text(q.serviceName),
                                  Text('Status: ${q.displayStatus}'),
                                  Text('Event: ${q.eventType ?? '—'}'),
                                  Text(q.displayRange),
                                  Text('Message: ${q.customerMessage ?? '—'}'),
                                  Text('Quoted price: ${q.displayQuotedPrice}'),
                                  Text('Vendor terms: ${q.vendorTerms ?? '—'}'),
                                ],
                              ),
                            ),
                          ),
                        ),
                      if (_auth != null)
                        TextButton(
                          onPressed: () => Navigator.of(context).pushNamed(
                            '/marketplace',
                            arguments: _auth,
                          ),
                          child: const Text('Browse marketplace'),
                        ),
                    ],
                  ),
                ),
    );
  }
}
