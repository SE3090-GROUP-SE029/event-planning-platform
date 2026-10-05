import 'package:dio/dio.dart';

import '../../../core/api/dio_client.dart';
import '../models/vendor_recommendation_model.dart';

class VendorRecommendationRemoteDataSource {
  VendorRecommendationRemoteDataSource({Dio? dio}) : dio = dio ?? DioClient().dio;

  final Dio dio;

  Options _auth(String token) => Options(headers: {'Authorization': 'Bearer $token'});

  Map<String, dynamic> _payload(Response<dynamic> response) {
    final body = response.data;
    if (body is Map<String, dynamic>) {
      final data = body['data'];
      if (data is Map<String, dynamic>) return data;
      if (body['success'] == false) {
        throw DioException(
          requestOptions: response.requestOptions,
          response: response,
          message: body['message']?.toString() ?? 'Request failed',
        );
      }
    }
    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      message: 'Unexpected recommendation response.',
    );
  }

  Future<VendorRecommendationRun> generate(
    String token,
    String eventId, {
    String? planId,
    bool forceRefresh = false,
    CancelToken? statusCancelToken,
    void Function(VendorRecommendationRun run)? onProgress,
    void Function()? onRetry,
  }) async {
    final response = await dio.post(
      '/api/events/$eventId/vendor-recommendations',
      data: {
        if (planId != null && planId.isNotEmpty) 'planId': planId,
        if (forceRefresh) 'forceRefresh': true,
      },
      options: _auth(token),
    );
    return waitForRun(
      token,
      eventId,
      VendorRecommendationRun.fromJson(_payload(response)),
      cancelToken: statusCancelToken,
      onProgress: onProgress,
      onRetry: onRetry,
    );
  }

  Future<VendorRecommendationRun?> getLatest(
    String token,
    String eventId, {
    CancelToken? cancelToken,
    void Function()? onRetry,
  }) async {
    while (true) {
      try {
        final response = await dio.get(
          '/api/events/$eventId/vendor-recommendations',
          options: _auth(token),
          cancelToken: cancelToken,
        );
        return VendorRecommendationRun.fromJson(_payload(response));
      } on DioException catch (error) {
        if (error.response?.statusCode == 404) return null;
        if (!_isTransient(error)) rethrow;
        onRetry?.call();
        await Future<void>.delayed(const Duration(seconds: 2));
      }
    }
  }

  Future<VendorRecommendationRun> getRun(
    String token,
    String eventId,
    String runId, {
    CancelToken? cancelToken,
  }
  ) async {
    final response = await dio.get(
      '/api/events/$eventId/vendor-recommendations/$runId',
      options: _auth(token),
      cancelToken: cancelToken,
    );
    return VendorRecommendationRun.fromJson(_payload(response));
  }

  Future<VendorRecommendationRun> waitForRun(
    String token,
    String eventId,
    VendorRecommendationRun run, {
    CancelToken? cancelToken,
    void Function(VendorRecommendationRun run)? onProgress,
    void Function()? onRetry,
  }) async {
    var current = run;
    onProgress?.call(current);
    while (current.isPending || current.isRunning) {
      await Future<void>.delayed(const Duration(seconds: 2));
      try {
        current = await getRun(
          token,
          eventId,
          current.id,
          cancelToken: cancelToken,
        );
        onProgress?.call(current);
      } on DioException catch (error) {
        if (!_isTransient(error)) rethrow;
        onRetry?.call();
      }
    }

    if (current.isFailed) {
      throw VendorRecommendationFailedException(
        current.failureMessage ?? 'Vendor recommendation failed. Please retry.',
      );
    }
    if (!current.isCompleted || current.items.isEmpty) {
      throw const FormatException(
        'Completed recommendation run has no recommendations.',
      );
    }
    return current;
  }

  bool _isTransient(DioException error) =>
      error.type == DioExceptionType.connectionTimeout ||
      error.type == DioExceptionType.receiveTimeout ||
      error.type == DioExceptionType.connectionError ||
      (error.response?.statusCode != null &&
          error.response!.statusCode! >= 500);
}

class VendorRecommendationFailedException implements Exception {
  final String message;

  const VendorRecommendationFailedException(this.message);

  @override
  String toString() => message;
}
