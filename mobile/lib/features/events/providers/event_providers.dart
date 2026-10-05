import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import '../api/event_remote_datasource.dart';
import '../models/event_model.dart';

final eventApiProvider = Provider((ref) => EventRemoteDataSource());
final eventListProvider = ChangeNotifierProvider.autoDispose
    .family<EventListController, String>((ref, token) {
  return EventListController(ref.read(eventApiProvider), token);
});

class EventListController extends ChangeNotifier {
  final EventRemoteDataSource api;
  final String token;
  final List<EventModel> events = [];
  bool loading = false, loadingMore = false, hasError = false;
  String? error;
  int page = 1, totalPages = 0;
  EventType? typeFilter;
  EventStatus? statusFilter;
  String searchQuery = '';
  String sortBy = 'createdAt', sortOrder = 'desc';
  int _requestGeneration = 0;

  EventListController(this.api, this.token);

  Future<void> load({bool refresh = false}) async {
    if (!refresh && (loading || loadingMore)) return;
    if (refresh) {
      _requestGeneration++;
      page = 1;
      totalPages = 0;
      events.clear();
      error = null;
    }
    if (!refresh && totalPages > 0 && page > totalPages) return;
    final requestGeneration = _requestGeneration;
    loading = events.isEmpty;
    loadingMore = events.isNotEmpty;
    hasError = false;
    notifyListeners();
    try {
      final result = await api.list(token,
          page: page,
          type: typeFilter,
          status: statusFilter,
          search: searchQuery,
          sortBy: sortBy,
          sortOrder: sortOrder);
      if (requestGeneration != _requestGeneration) return;
      if (page == 1) events.clear();
      events.addAll(result.items);
      totalPages = result.totalPages;
      page++;
    } catch (e) {
      if (requestGeneration == _requestGeneration) {
        hasError = true;
        error = e.toString();
      }
    } finally {
      if (requestGeneration == _requestGeneration) {
        loading = false;
        loadingMore = false;
        notifyListeners();
      }
    }
  }

  Future<void> delete(EventModel event) async {
    await api.delete(token, event.id);
    events.removeWhere((item) => item.id == event.id);
    notifyListeners();
  }
}

EventModel emptyEvent() => EventModel(
      id: '',
      ownerId: '',
      eventName: '',
      eventType: EventType.wedding,
      guestCount: 1,
      budget: 0,
      preferredVenue: '',
      preferredDate: DateTime.now().add(const Duration(days: 1)),
      startTime: const Duration(hours: 18),
      endTime: const Duration(hours: 23),
      eventDuration: const Duration(hours: 1),
      requirements: null,
      status: EventStatus.draft,
      createdAt: DateTime.now(),
      updatedAt: null,
    );
