import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/booking_remote_datasource.dart';
import '../models/booking_model.dart';

class VendorBookingsPage extends StatefulWidget {
  const VendorBookingsPage({super.key});

  @override
  State<VendorBookingsPage> createState() => _VendorBookingsPageState();
}

class _VendorBookingsPageState extends State<VendorBookingsPage> {
  final _api = BookingRemoteDataSource();
  AuthResponseModel? _auth;
  List<BookingModel> _items = [];
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
      final items = await _api.listForVendor(auth.accessToken);
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
      appBar: AppBar(title: const Text('Bookings')),
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
                      const PastelSectionHeader(title: 'Your business bookings'),
                      if (_items.isEmpty)
                        const PastelCard(
                          padding: EdgeInsets.all(AppDimens.space20),
                          child: Text('No bookings yet.'),
                        )
                      else
                        ..._items.map(
                          (b) => Padding(
                            padding: const EdgeInsets.only(bottom: AppDimens.space12),
                            child: PastelCard(
                              padding: const EdgeInsets.all(AppDimens.space16),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    b.serviceName,
                                    style: const TextStyle(fontWeight: FontWeight.w800),
                                  ),
                                  Text('Status: ${b.status}'),
                                  Text('Event: ${b.eventType ?? '—'}'),
                                  Text(b.displayRange),
                                  Text('Agreed price: ${b.displayAgreedPrice}'),
                                  TextButton(
                                    onPressed: () => Navigator.of(context).pushNamed(
                                      '/bookings/details',
                                      arguments: {
                                        'auth': _auth,
                                        'bookingId': b.id,
                                        'vendorMode': true,
                                      },
                                    ),
                                    child: const Text('Manage booking'),
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
