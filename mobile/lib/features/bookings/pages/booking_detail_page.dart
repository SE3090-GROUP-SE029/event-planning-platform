import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/booking_remote_datasource.dart';
import '../models/booking_model.dart';
import '../models/vendor_rating_model.dart';

class BookingDetailPage extends StatefulWidget {
  const BookingDetailPage({super.key});

  @override
  State<BookingDetailPage> createState() => _BookingDetailPageState();
}

class _BookingDetailPageState extends State<BookingDetailPage> {
  final _api = BookingRemoteDataSource();
  final _reasonController = TextEditingController();
  final _commentController = TextEditingController();
  AuthResponseModel? _auth;
  BookingModel? _booking;
  VendorRatingModel? _review;
  bool _vendorMode = false;
  bool _loading = true;
  bool _busy = false;
  int _selectedRating = 5;
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
    _reasonController.dispose();
    _commentController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final args = ModalRoute.of(context)?.settings.arguments;
    if (args is! Map) {
      setState(() {
        _loading = false;
        _error = 'Missing booking details.';
      });
      return;
    }

    final auth = args['auth'];
    final bookingId = args['bookingId']?.toString();
    final vendorMode = args['vendorMode'] == true;

    if (auth is! AuthResponseModel ||
        auth.accessToken.isEmpty ||
        bookingId == null ||
        bookingId.isEmpty) {
      setState(() {
        _loading = false;
        _error = 'Missing booking details.';
      });
      return;
    }

    try {
      final booking = await _api.getById(auth.accessToken, bookingId);
      VendorRatingModel? review;
      if (booking.hasReview) {
        try {
          review = await _api.getReview(auth.accessToken, bookingId);
        } catch (_) {
          review = null;
        }
      }
      if (!mounted) return;
      setState(() {
        _auth = auth;
        _booking = booking;
        _review = review;
        _vendorMode = vendorMode;
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

  Future<void> _complete() async {
    final auth = _auth;
    final booking = _booking;
    if (auth == null || booking == null) return;
    setState(() => _busy = true);
    try {
      final updated = await _api.complete(auth.accessToken, booking.id);
      if (!mounted) return;
      setState(() {
        _booking = updated;
        _busy = false;
        _error = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _busy = false;
        _error = e.toString();
      });
    }
  }

  Future<void> _cancel() async {
    final auth = _auth;
    final booking = _booking;
    if (auth == null || booking == null) return;
    final reason = _reasonController.text.trim();
    if (reason.isEmpty) {
      setState(() => _error = 'Cancellation reason is required.');
      return;
    }
    setState(() => _busy = true);
    try {
      final updated = await _api.cancel(
        auth.accessToken,
        booking.id,
        cancellationReason: reason,
      );
      if (!mounted) return;
      setState(() {
        _booking = updated;
        _busy = false;
        _error = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _busy = false;
        _error = e.toString();
      });
    }
  }

  Future<void> _submitReview() async {
    final auth = _auth;
    final booking = _booking;
    if (auth == null || booking == null) return;
    setState(() => _busy = true);
    try {
      final review = await _api.createReview(
        auth.accessToken,
        booking.id,
        rating: _selectedRating,
        comment: _commentController.text.trim().isEmpty
            ? null
            : _commentController.text.trim(),
      );
      final refreshed = await _api.getById(auth.accessToken, booking.id);
      if (!mounted) return;
      setState(() {
        _review = review;
        _booking = refreshed;
        _busy = false;
        _error = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _busy = false;
        _error = e.toString();
      });
    }
  }

  bool get _canWriteReview {
    final booking = _booking;
    if (booking == null || _vendorMode) return false;
    return booking.status == 'COMPLETED' && !booking.hasReview && _review == null;
  }

  @override
  Widget build(BuildContext context) {
    final b = _booking;
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(title: const Text('Booking details')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && b == null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Text(_error!, style: const TextStyle(color: AppColors.error)),
                  ),
                )
              : ListView(
                  padding: const EdgeInsets.all(AppDimens.space20),
                  children: [
                    if (_error != null)
                      Padding(
                        padding: const EdgeInsets.only(bottom: AppDimens.space12),
                        child: Text(_error!, style: const TextStyle(color: AppColors.error)),
                      ),
                    if (b != null)
                      PastelCard(
                        padding: const EdgeInsets.all(AppDimens.space16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              b.vendorBusinessName,
                              style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
                            ),
                            Text(b.serviceName),
                            const SizedBox(height: 8),
                            Text('Status: ${b.status}'),
                            Text('Event: ${b.eventType ?? '—'}'),
                            Text(b.displayRange),
                            Text('Agreed price: ${b.displayAgreedPrice}'),
                            Text('Vendor terms: ${b.vendorTerms ?? '—'}'),
                            if (b.cancellationReason != null)
                              Text('Cancellation reason: ${b.cancellationReason}'),
                            if (b.status == 'CONFIRMED') ...[
                              const SizedBox(height: 16),
                              if (_vendorMode)
                                ElevatedButton(
                                  onPressed: _busy ? null : _complete,
                                  child: const Text('Mark completed'),
                                ),
                              const SizedBox(height: 8),
                              TextField(
                                controller: _reasonController,
                                decoration: const InputDecoration(
                                  labelText: 'Cancellation reason',
                                ),
                                maxLines: 2,
                              ),
                              const SizedBox(height: 8),
                              OutlinedButton(
                                onPressed: _busy ? null : _cancel,
                                child: const Text('Cancel booking'),
                              ),
                            ],
                          ],
                        ),
                      ),
                    if (_review != null) ...[
                      const SizedBox(height: AppDimens.space12),
                      PastelCard(
                        padding: const EdgeInsets.all(AppDimens.space16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Your review',
                              style: TextStyle(fontWeight: FontWeight.w800),
                            ),
                            const SizedBox(height: 8),
                            Text(_review!.displayStars),
                            Text(_review!.comment?.isNotEmpty == true
                                ? _review!.comment!
                                : 'No comment'),
                          ],
                        ),
                      ),
                    ],
                    if (_canWriteReview) ...[
                      const SizedBox(height: AppDimens.space12),
                      PastelCard(
                        padding: const EdgeInsets.all(AppDimens.space16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Write review',
                              style: TextStyle(fontWeight: FontWeight.w800),
                            ),
                            const SizedBox(height: 8),
                            const Text('Rating'),
                            const SizedBox(height: 4),
                            Row(
                              children: List.generate(5, (index) {
                                final value = index + 1;
                                final selected = value <= _selectedRating;
                                return IconButton(
                                  onPressed: _busy
                                      ? null
                                      : () => setState(() => _selectedRating = value),
                                  icon: Icon(
                                    selected ? Icons.star : Icons.star_border,
                                    color: selected ? Colors.amber.shade700 : AppColors.textSecondary,
                                  ),
                                );
                              }),
                            ),
                            Text('Selected: $_selectedRating / 5'),
                            const SizedBox(height: 8),
                            TextField(
                              controller: _commentController,
                              decoration: const InputDecoration(
                                labelText: 'Comment (optional)',
                              ),
                              maxLines: 3,
                            ),
                            const SizedBox(height: 12),
                            ElevatedButton(
                              onPressed: _busy ? null : _submitReview,
                              child: const Text('Submit review'),
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
