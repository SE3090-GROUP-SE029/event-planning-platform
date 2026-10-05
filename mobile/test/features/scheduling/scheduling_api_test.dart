import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/scheduling/api/scheduling_api.dart';

void main() {
  test('AI schedule generation allows the backend long-running timeout',
      () async {
    final adapter = _CapturingAdapter();
    final cancelToken = CancelToken();
    final dio = Dio(
      BaseOptions(
        baseUrl: 'https://api.example.test',
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 10),
      ),
    )..httpClientAdapter = adapter;

    final schedule = await SchedulingApi(dio: dio).generateAiSchedule(
      'schedule-id',
      cancelToken: cancelToken,
    );

    expect(
        adapter.requestOptions?.path, '/api/schedules/schedule-id/generate-ai');
    expect(
      adapter.requestOptions?.connectTimeout,
      const Duration(minutes: 30),
    );
    expect(
      adapter.requestOptions?.receiveTimeout,
      const Duration(minutes: 30),
    );
    expect(
      adapter.requestOptions?.sendTimeout,
      const Duration(minutes: 30),
    );
    expect(adapter.requestOptions?.cancelToken, same(cancelToken));
    expect(dio.options.connectTimeout, const Duration(seconds: 10));
    expect(dio.options.receiveTimeout, const Duration(seconds: 10));
    expect(dio.options.sendTimeout, isNull);
    expect(schedule.id, 'schedule-id');
    expect(schedule.activities, isEmpty);
  });

  test('AI schedule generation reports timeout-specific message', () async {
    final dio = Dio(
      BaseOptions(baseUrl: 'https://api.example.test'),
    )..httpClientAdapter = _CapturingAdapter(
        failureType: DioExceptionType.receiveTimeout,
      );

    await expectLater(
      SchedulingApi(dio: dio).generateAiSchedule('schedule-id'),
      throwsA(
        isA<Exception>().having(
          (error) => error.toString(),
          'message',
          contains(
            'AI schedule generation exceeded the maximum allowed processing time.',
          ),
        ),
      ),
    );
  });

  test('AI schedule generation preserves the backend timeout message',
      () async {
    const timeoutMessage =
        'AI schedule generation exceeded the maximum allowed processing time.';
    final dio = Dio(
      BaseOptions(baseUrl: 'https://api.example.test'),
    )..httpClientAdapter = _CapturingAdapter(
        failureStatusCode: 504,
        failureMessage: timeoutMessage,
      );

    await expectLater(
      SchedulingApi(dio: dio).generateAiSchedule('schedule-id'),
      throwsA(
        isA<Exception>().having(
          (error) => error.toString(),
          'message',
          contains(timeoutMessage),
        ),
      ),
    );
  });
}

class _CapturingAdapter implements HttpClientAdapter {
  _CapturingAdapter({
    this.failureType,
    this.failureStatusCode,
    this.failureMessage,
  });

  final DioExceptionType? failureType;
  final int? failureStatusCode;
  final String? failureMessage;
  RequestOptions? requestOptions;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requestOptions = options;
    if (failureType != null) {
      throw DioException(
        requestOptions: options,
        type: failureType!,
        message: 'Simulated request timeout',
      );
    }
    if (failureStatusCode != null) {
      throw DioException(
        requestOptions: options,
        type: DioExceptionType.badResponse,
        response: Response(
          requestOptions: options,
          statusCode: failureStatusCode,
          data: {'message': failureMessage},
        ),
      );
    }
    return ResponseBody.fromString(
      '{"id":"schedule-id","eventId":"event-id","isLocked":false,'
      '"activities":[],"conflicts":[]}',
      200,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}
