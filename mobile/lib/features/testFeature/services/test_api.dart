import 'package:dio/dio.dart';
import '../../../core/api/dio_client.dart';
import '../models/test_models.dart';

class TestApi {
  final _client = DioClient().dio;

  Future<PingResponse> ping() async {
    try {
      final response = await _client.get<Map<String, dynamic>>('/api/test/ping');
      return PingResponse.fromJson(response.data!);
    } on DioException catch (e) {
      throw Exception('Ping failed: ${e.message}');
    }
  }

  Future<TestMessage> createMessage(String message) async {
    try {
      final request = CreateTestMessageRequest(message: message);
      final response = await _client.post<Map<String, dynamic>>(
        '/api/test/message',
        data: request.toJson(),
      );
      return TestMessage.fromJson(response.data!);
    } on DioException catch (e) {
      throw Exception('Create message failed: ${e.message}');
    }
  }

  Future<TestMessage> getMessage(int id) async {
    try {
      final response = await _client.get<Map<String, dynamic>>('/api/test/message/$id');
      return TestMessage.fromJson(response.data!);
    } on DioException catch (e) {
      throw Exception('Get message failed: ${e.message}');
    }
  }
}
