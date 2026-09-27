import 'package:dio/dio.dart';

import '../../../core/api/dio_client.dart';
import '../models/booking_model.dart';

class BookingRemoteDataSource {
  final Dio dio;

  BookingRemoteDataSource({Dio? dio}) : dio = dio ?? DioClient().dio;

  Options _auth(String accessToken) => Options(
        headers: {'Authorization': 'Bearer $accessToken'},
      );

  Future<BookingModel> acceptQuotation(String accessToken, String quotationId) async {
    try {
      final response = await dio.post(
        '/api/quotations/$quotationId/accept',
        options: _auth(accessToken),
      );
      return BookingModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<List<BookingModel>> listMine(String accessToken) async {
    try {
      final response = await dio.get(
        '/api/bookings/mine',
        options: _auth(accessToken),
      );
      final data = response.data as List<dynamic>;
      return data
          .map((item) => BookingModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<List<BookingModel>> listForVendor(String accessToken) async {
    try {
      final response = await dio.get(
        '/api/bookings/vendor',
        options: _auth(accessToken),
      );
      final data = response.data as List<dynamic>;
      return data
          .map((item) => BookingModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<BookingModel> getById(String accessToken, String id) async {
    try {
      final response = await dio.get(
        '/api/bookings/$id',
        options: _auth(accessToken),
      );
      return BookingModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<BookingModel> complete(String accessToken, String id) async {
    try {
      final response = await dio.put(
        '/api/bookings/$id/complete',
        options: _auth(accessToken),
      );
      return BookingModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<BookingModel> cancel(
    String accessToken,
    String id, {
    required String cancellationReason,
  }) async {
    try {
      final response = await dio.put(
        '/api/bookings/$id/cancel',
        data: {'cancellationReason': cancellationReason},
        options: _auth(accessToken),
      );
      return BookingModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  String _handleError(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['message'] != null) {
      return data['message'].toString();
    }
    return e.message ?? 'Request failed';
  }
}
