import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../auth/models/auth_response_model.dart';
import '../../auth/providers/auth_providers.dart';
import '../api/vendor_recommendation_remote_datasource.dart';
import '../models/vendor_recommendation_model.dart';

class VendorRecommendationsPage extends ConsumerStatefulWidget {
  final String eventId;
  final String? planId;

  const VendorRecommendationsPage({
    super.key,
    required this.eventId,
    this.planId,
  });

  @override
  ConsumerState<VendorRecommendationsPage> createState() =>
      _VendorRecommendationsPageState();
}

class _VendorRecommendationsPageState
    extends ConsumerState<VendorRecommendationsPage> {
  final _api = VendorRecommendationRemoteDataSource();
  VendorRecommendationRun? _run;
  bool _loading = true;
  bool _generating = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _loadInitial());
  }

  Future<void> _loadInitial() async {
    final session = ref.read(currentUserProvider);
    if (session == null) {
      setState(() {
        _loading = false;
        _error = 'Please sign in to view recommendations.';
      });
      return;
    }

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final latest = await _api.getLatest(session.accessToken, widget.eventId);
      if (!mounted) return;
      if (latest != null) {
        setState(() {
          _run = latest;
          _loading = false;
        });
        return;
      }
      await _generate(session);
    } on DioException catch (error) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = _messageFor(error);
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = 'Could not load vendor recommendations.';
      });
    }
  }

  Future<void> _generate(AuthResponseModel session) async {
    setState(() {
      _generating = true;
      _error = null;
    });
    try {
      final run = await _api.generate(
        session.accessToken,
        widget.eventId,
        planId: widget.planId,
      );
      if (!mounted) return;
      setState(() {
        _run = run;
        _loading = false;
        _generating = false;
      });
    } on DioException catch (error) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _generating = false;
        _error = _messageFor(error);
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _generating = false;
        _error = 'Vendor recommendation failed. Please try again.';
      });
    }
  }

  String _messageFor(DioException error) {
    final data = error.response?.data;
    if (data is Map && data['message'] != null) {
      return data['message'].toString();
    }
    return switch (error.response?.statusCode) {
      404 => 'No recommendations available yet.',
      503 || 502 || 504 =>
        'The AI recommendation service is temporarily unavailable.',
      _ => 'Could not load vendor recommendations.',
    };
  }

  String _money(double? value) {
    if (value == null) return 'Price on request';
    return 'LKR ${value.toStringAsFixed(0)}';
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(currentUserProvider);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Vendor recommendations'),
        actions: [
          if (session != null)
            IconButton(
              tooltip: 'Refresh recommendations',
              onPressed: _generating || _loading
                  ? null
                  : () => _generate(session),
              icon: const Icon(Icons.refresh),
            ),
        ],
      ),
      body: _buildBody(session),
    );
  }

  Widget _buildBody(AuthResponseModel? session) {
    if (_loading || _generating && _run == null) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_run == null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(_error ?? 'No recommendations yet.', textAlign: TextAlign.center),
              const SizedBox(height: 16),
              if (session != null)
                FilledButton(
                  onPressed: _generating ? null : () => _generate(session),
                  child: const Text('Generate recommendations'),
                ),
            ],
          ),
        ),
      );
    }

    final run = _run!;
    return RefreshIndicator(
      onRefresh: session == null ? () async {} : () => _generate(session),
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          if (run.fromCache || (run.sourceNote?.isNotEmpty ?? false))
            Card(
              color: AppColors.warning.withValues(alpha: 0.15),
              child: ListTile(
                leading: const Icon(Icons.info_outline),
                title: Text(
                  run.sourceNote ??
                      'Showing previously saved recommendations.',
                ),
              ),
            ),
          Text(
            'Matched to your approved plan',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: 4),
          Text(
            '${run.items.length} recommendation${run.items.length == 1 ? '' : 's'} · ${run.candidateCount} candidates scored',
            style: Theme.of(context).textTheme.bodyMedium,
          ),
          if (_error != null) ...[
            const SizedBox(height: 8),
            Text(_error!, style: const TextStyle(color: AppColors.error)),
          ],
          if (_generating) ...[
            const SizedBox(height: 12),
            const LinearProgressIndicator(),
          ],
          const SizedBox(height: 12),
          ...run.items.map((item) => _RecommendationTile(
                item: item,
                money: _money,
                onOpen: session == null
                    ? null
                    : () {
                        Navigator.of(context).pushNamed(
                          '/marketplace/details',
                          arguments: {
                            'auth': session,
                            'vendorId': item.vendorId,
                          },
                        );
                      },
              )),
        ],
      ),
    );
  }
}

class _RecommendationTile extends StatelessWidget {
  final VendorRecommendationItem item;
  final String Function(double?) money;
  final VoidCallback? onOpen;

  const _RecommendationTile({
    required this.item,
    required this.money,
    required this.onOpen,
  });

  @override
  Widget build(BuildContext context) {
    final ratingLabel = item.averageRating == null
        ? 'No ratings yet'
        : '${item.averageRating!.toStringAsFixed(1)} ★ (${item.reviewCount})';

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: InkWell(
        onTap: onOpen,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      item.businessName,
                      style: const TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  Chip(label: Text('Score ${item.score}')),
                ],
              ),
              const SizedBox(height: 6),
              Text(item.serviceName ?? item.category),
              const SizedBox(height: 4),
              Text('${money(item.price)}${item.pricingType == null ? '' : ' · ${item.pricingType}'}'),
              Text(ratingLabel),
              if (item.availabilityMatch)
                const Padding(
                  padding: EdgeInsets.only(top: 4),
                  child: Text('Available on event date'),
                ),
              const SizedBox(height: 8),
              Text(
                item.reason,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
