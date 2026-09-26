import 'package:dio/dio.dart';

import '../../../core/api/dio_client.dart';
import '../../events/models/event_model.dart';
import '../models/quotation_model.dart';

class QuotationRemoteDataSource {
  final Dio dio;

  QuotationRemoteDataSource({Dio? dio}) : dio = dio ?? DioClient().dio;

  Options _auth(String accessToken) => Options(
        headers: {'Authorization': 'Bearer $accessToken'},
      );

  Future<List<EventModel>> listMyEvents(String accessToken) async {
    try {
      final response = await dio.get(
        '/api/events',
        queryParameters: {'page': 1, 'pageSize': 100},
        options: _auth(accessToken),
      );
      final data = response.data as Map<String, dynamic>;
      final items = data['items'] as List<dynamic>? ?? [];
      return items
          .map((item) => EventModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<QuotationModel> create(
    String accessToken, {
    required String vendorId,
    required String vendorServiceId,
    required String eventId,
    required DateTime requestedStartDateTime,
    required DateTime requestedEndDateTime,
    String? customerMessage,
  }) async {
    try {
      final response = await dio.post(
        '/api/quotations',
        data: {
          'vendorId': vendorId,
          'vendorServiceId': vendorServiceId,
          'eventId': eventId,
          'requestedStartDateTime':
              requestedStartDateTime.toUtc().toIso8601String(),
          'requestedEndDateTime':
              requestedEndDateTime.toUtc().toIso8601String(),
          'customerMessage': customerMessage,
        },
        options: _auth(accessToken),
      );
      return QuotationModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<List<QuotationModel>> listMine(String accessToken) async {
    try {
      final response = await dio.get(
        '/api/quotations/mine',
        options: _auth(accessToken),
      );
      final data = response.data as List<dynamic>;
      return data
          .map((item) => QuotationModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<List<QuotationModel>> listForVendor(String accessToken) async {
    try {
      final response = await dio.get(
        '/api/quotations/vendor',
        options: _auth(accessToken),
      );
      final data = response.data as List<dynamic>;
      return data
          .map((item) => QuotationModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<QuotationModel> getById(String accessToken, String id) async {
    try {
      final response = await dio.get(
        '/api/quotations/$id',
        options: _auth(accessToken),
      );
      return QuotationModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<QuotationModel> respond(
    String accessToken,
    String id, {
    required double quotedPrice,
    String? vendorTerms,
  }) async {
    try {
      final response = await dio.put(
        '/api/quotations/$id/respond',
        data: {
          'quotedPrice': quotedPrice,
          'vendorTerms': vendorTerms,
        },
        options: _auth(accessToken),
      );
      return QuotationModel.fromJson(response.data as Map<String, dynamic>);
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
