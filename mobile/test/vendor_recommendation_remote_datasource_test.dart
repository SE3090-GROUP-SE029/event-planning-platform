import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/recommendations/api/vendor_recommendation_remote_datasource.dart';

void main() {
  test('generation polls an accepted run until recommendations are completed',
      () async {
    final adapter = _RecommendationAdapter();
    final dio = Dio(BaseOptions(baseUrl: 'https://api.example.test'))
      ..httpClientAdapter = adapter;
    final progressStatuses = <String>[];

    final result =
        await VendorRecommendationRemoteDataSource(dio: dio).generate(
      'access-token',
      'event-id',
      onProgress: (run) => progressStatuses.add(run.status),
    );

    expect(adapter.methods, ['POST', 'GET']);
    expect(progressStatuses, ['Pending', 'Completed']);
    expect(result.isCompleted, isTrue);
    expect(result.items, hasLength(1));
    expect(result.items.single.businessName, 'Taste Co');
  });

  test('transient polling timeouts do not mark a running job as failed',
      () async {
    final adapter = _RecommendationAdapter(transientStatusFailures: 1);
    final dio = Dio(BaseOptions(baseUrl: 'https://api.example.test'))
      ..httpClientAdapter = adapter;
    var retryReported = false;

    final result =
        await VendorRecommendationRemoteDataSource(dio: dio).generate(
      'access-token',
      'event-id',
      onRetry: () => retryReported = true,
    );

    expect(adapter.methods, ['POST', 'GET', 'GET']);
    expect(retryReported, isTrue);
    expect(result.isCompleted, isTrue);
  });
}

class _RecommendationAdapter implements HttpClientAdapter {
  _RecommendationAdapter({this.transientStatusFailures = 0});

  int transientStatusFailures;
  final methods = <String>[];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    methods.add(options.method);
    if (options.method == 'GET' && transientStatusFailures > 0) {
      transientStatusFailures--;
      throw DioException(
        requestOptions: options,
        type: DioExceptionType.receiveTimeout,
        message: 'Simulated status polling timeout',
      );
    }

    final response = options.method == 'POST'
        ? _run(status: 'Pending', items: const [])
        : _run(
            status: 'Completed',
            items: [
              {
                'vendorId': 'vendor-id',
                'rank': 1,
                'score': 90,
                'reason': 'Matches the plan.',
                'businessName': 'Taste Co',
                'category': 'CATERING',
                'reviewCount': 0,
                'availabilityMatch': true,
              }
            ],
          );
    return ResponseBody.fromString(
      jsonEncode({
        'success': true,
        'data': response,
      }),
      options.method == 'POST' ? 202 : 200,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  Map<String, dynamic> _run({
    required String status,
    required List<Map<String, dynamic>> items,
  }) =>
      {
        'id': 'run-id',
        'eventId': 'event-id',
        'eventPlanDraftId': 'plan-id',
        'candidateCount': 1,
        'createdAt': '2026-10-05T12:00:00Z',
        'status': status,
        'stage':
            status == 'Pending' ? 'Preparing recommendations' : 'Completed',
        'items': items,
      };

  @override
  void close({bool force = false}) {}
}
