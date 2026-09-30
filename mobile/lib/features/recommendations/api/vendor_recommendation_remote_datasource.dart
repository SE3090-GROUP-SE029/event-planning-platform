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
  }) async {
    final response = await dio.post(
      '/api/events/$eventId/vendor-recommendations',
      data: planId == null || planId.isEmpty ? <String, dynamic>{} : {'planId': planId},
      options: _auth(token),
    );
    return VendorRecommendationRun.fromJson(_payload(response));
  }

  Future<VendorRecommendationRun?> getLatest(String token, String eventId) async {
    try {
      final response = await dio.get(
        '/api/events/$eventId/vendor-recommendations',
        options: _auth(token),
      );
      return VendorRecommendationRun.fromJson(_payload(response));
    } on DioException catch (error) {
      if (error.response?.statusCode == 404) return null;
      rethrow;
    }
  }
}
