import 'package:dio/dio.dart';
import '../../../core/api/dio_client.dart';
import '../models/event_model.dart';

class EventRemoteDataSource {
  final Dio dio;
  EventRemoteDataSource({Dio? dio}) : dio = dio ?? DioClient().dio;

  Options _auth(String token) =>
      Options(headers: {'Authorization': 'Bearer $token'});

  Future<EventModel> create(String token, EventModel event) async {
    final response = await dio.post('/api/events',
        data: event.toCreateJson(), options: _auth(token));
    return EventModel.fromJson(response.data as Map<String, dynamic>);
  }

  Future<EventPage> list(String token,
      {int page = 1,
      int pageSize = 10,
      EventType? type,
      EventStatus? status,
      String sortBy = 'createdAt',
      String sortOrder = 'desc'}) async {
    final response = await dio.get('/api/events',
        queryParameters: {
          'page': page,
          'pageSize': pageSize,
          'sortBy': sortBy,
          'sortOrder': sortOrder,
          if (type != null) 'eventType': _enumName(type),
          if (status != null) 'status': _enumName(status),
        },
        options: _auth(token));
    return EventPage.fromJson(response.data as Map<String, dynamic>);
  }

  Future<EventModel> get(String token, String id) async {
    final response = await dio.get('/api/events/$id', options: _auth(token));
    return EventModel.fromJson(response.data as Map<String, dynamic>);
  }

  Future<EventModel> update(String token, String id, EventModel event) async {
    final response = await dio.put('/api/events/$id',
        data: event.toUpdateJson(), options: _auth(token));
    return EventModel.fromJson(response.data as Map<String, dynamic>);
  }

  Future<void> delete(String token, String id) async {
    await dio.delete('/api/events/$id', options: _auth(token));
  }
}

String _enumName(Object value) => value.toString().split('.').last;
