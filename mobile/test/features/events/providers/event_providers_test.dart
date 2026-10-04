import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/events/api/event_remote_datasource.dart';
import 'package:mobile/features/events/models/event_model.dart';
import 'package:mobile/features/events/providers/event_providers.dart';

void main() {
  test('event list sends the active search term to its data source', () async {
    final dataSource = _TestEventRemoteDataSource();
    final controller = EventListController(dataSource, 'token');
    controller.searchQuery = 'Autumn celebration';

    await controller.load(refresh: true);

    expect(dataSource.lastSearch, 'Autumn celebration');
    expect(controller.events, isEmpty);
    controller.dispose();
  });
}

class _TestEventRemoteDataSource extends EventRemoteDataSource {
  String? lastSearch;

  _TestEventRemoteDataSource() : super(dio: Dio());

  @override
  Future<EventPage> list(
    String token, {
    int page = 1,
    int pageSize = 10,
    EventType? type,
    EventStatus? status,
    String? search,
    String sortBy = 'createdAt',
    String sortOrder = 'desc',
  }) async {
    lastSearch = search;
    return const EventPage(
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    );
  }
}
