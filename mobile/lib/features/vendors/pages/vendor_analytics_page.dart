import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/vendor_remote_datasource.dart';
import '../models/vendor_analytics_model.dart';

class VendorAnalyticsPage extends StatefulWidget {
  const VendorAnalyticsPage({super.key});

  @override
  State<VendorAnalyticsPage> createState() => _VendorAnalyticsPageState();
}

class _VendorAnalyticsPageState extends State<VendorAnalyticsPage> {
  final _api = VendorRemoteDataSource();
  AuthResponseModel? _auth;
  VendorAnalyticsModel? _analytics;
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
      final analytics = await _api.getMyAnalytics(auth.accessToken);
      if (!mounted) return;
      setState(() {
        _auth = auth;
        _analytics = analytics;
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

  Widget _metricCard(String label, String value) {
    return Expanded(
      child: PastelCard(
        padding: const EdgeInsets.all(AppDimens.space14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              label,
              style: const TextStyle(
                color: AppColors.textSecondary,
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              value,
              style: const TextStyle(
                fontWeight: FontWeight.w800,
                fontSize: 18,
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final data = _analytics;
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(title: const Text('Vendor analytics')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && data == null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Text(
                      _error!,
                      style: const TextStyle(color: AppColors.error),
                    ),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: () async {
                    setState(() => _loading = true);
                    await _load();
                  },
                  child: ListView(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    children: [
                      if (_error != null)
                        Padding(
                          padding:
                              const EdgeInsets.only(bottom: AppDimens.space12),
                          child: Text(
                            _error!,
                            style: const TextStyle(color: AppColors.error),
                          ),
                        ),
                      if (data != null) ...[
                        const PastelSectionHeader(title: 'Bookings summary'),
                        Row(
                          children: [
                            _metricCard('Total', '${data.totalBookings}'),
                            const SizedBox(width: AppDimens.space10),
                            _metricCard(
                                'Confirmed', '${data.confirmedBookings}'),
                          ],
                        ),
                        const SizedBox(height: AppDimens.space10),
                        Row(
                          children: [
                            _metricCard(
                                'Completed', '${data.completedBookings}'),
                            const SizedBox(width: AppDimens.space10),
                            _metricCard(
                                'Cancelled', '${data.cancelledBookings}'),
                          ],
                        ),
                        const PastelSectionHeader(title: 'Revenue'),
                        PastelCard(
                          padding: const EdgeInsets.all(AppDimens.space16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                data.displayRevenue,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w800,
                                  fontSize: 22,
                                ),
                              ),
                              const SizedBox(height: 4),
                              const Text(
                                'From completed bookings only',
                                style: TextStyle(
                                  color: AppColors.textSecondary,
                                  fontSize: 12,
                                ),
                              ),
                            ],
                          ),
                        ),
                        const PastelSectionHeader(title: 'Ratings'),
                        PastelCard(
                          padding: const EdgeInsets.all(AppDimens.space16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                data.displayRating,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w800,
                                  fontSize: 18,
                                ),
                              ),
                              Text(
                                '${data.reviewCount} review${data.reviewCount == 1 ? '' : 's'}',
                                style: const TextStyle(
                                  color: AppColors.textSecondary,
                                ),
                              ),
                            ],
                          ),
                        ),
                        const PastelSectionHeader(title: 'Quotations'),
                        Row(
                          children: [
                            _metricCard('Requests', '${data.totalQuotations}'),
                            const SizedBox(width: AppDimens.space10),
                            _metricCard(
                                'Accepted', '${data.quotationsAccepted}'),
                          ],
                        ),
                        const PastelSectionHeader(title: 'Recent bookings'),
                        if (data.recentBookings.isEmpty)
                          const PastelCard(
                            padding: EdgeInsets.all(AppDimens.space16),
                            child: Text(
                              'No bookings yet.',
                              style: TextStyle(color: AppColors.textSecondary),
                            ),
                          )
                        else
                          ...data.recentBookings.map(
                            (booking) => Padding(
                              padding: const EdgeInsets.only(
                                bottom: AppDimens.space10,
                              ),
                              child: PastelCard(
                                padding:
                                    const EdgeInsets.all(AppDimens.space14),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      booking.serviceName,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w800,
                                      ),
                                    ),
                                    Text('Status: ${booking.status}'),
                                    Text(booking.displayRange),
                                    Text(booking.displayPrice),
                                    Text(
                                      booking.hasReview
                                          ? 'Reviewed'
                                          : 'Not reviewed',
                                      style: const TextStyle(
                                        color: AppColors.textSecondary,
                                        fontSize: 12,
                                      ),
                                    ),
                                    if (_auth != null)
                                      Align(
                                        alignment: Alignment.centerRight,
                                        child: TextButton(
                                          onPressed: () =>
                                              Navigator.of(context).pushNamed(
                                            '/bookings/details',
                                            arguments: {
                                              'auth': _auth,
                                              'bookingId': booking.id,
                                              'vendorMode': true,
                                            },
                                          ),
                                          child: const Text('Open'),
                                        ),
                                      ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                      ],
                    ],
                  ),
                ),
    );
  }
}
