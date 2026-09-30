import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import '../api/guest_management_remote_datasource.dart';
import '../models/guest_registration_model.dart';
import '../models/registration_form_model.dart';

// ──────────────────────────────────────────────────────
// Shared API provider
// ──────────────────────────────────────────────────────

final guestManagementApiProvider =
    Provider<GuestManagementRemoteDataSource>((ref) {
  return GuestManagementRemoteDataSource();
});

// ──────────────────────────────────────────────────────
// PLANNER — Guest list controller (ChangeNotifier)
// Following the exact same pattern as EventListController
// ──────────────────────────────────────────────────────

final guestListControllerProvider = ChangeNotifierProvider.autoDispose
    .family<GuestListController, String>((ref, eventId) {
  return GuestListController(ref.read(guestManagementApiProvider), eventId);
});

class GuestListController extends ChangeNotifier {
  final GuestManagementRemoteDataSource api;
  final String eventId;

  final List<PlannerRegistrationModel> guests = [];
  bool loading = false;
  bool loadingMore = false;
  bool hasError = false;
  String? error;

  int page = 1;
  int total = 0;
  int pageSize = 50;

  // Filters
  String? statusFilter;
  String? rsvpStatusFilter;
  bool? checkedInFilter;
  String searchQuery = '';

  GuestListController(this.api, this.eventId);

  int get totalPages => pageSize > 0 ? ((total + pageSize - 1) ~/ pageSize) : 0;
  bool get hasMore => page <= totalPages;

  Future<void> load({bool refresh = false}) async {
    if (loading || loadingMore) return;
    if (refresh) {
      page = 1;
      guests.clear();
    }
    if (!refresh && totalPages > 0 && page > totalPages) return;
    loading = guests.isEmpty;
    loadingMore = guests.isNotEmpty;
    hasError = false;
    notifyListeners();
    try {
      final result = await api.listRegistrations(
        eventId,
        page: page,
        pageSize: pageSize,
        status: statusFilter,
        rsvpStatus: rsvpStatusFilter,
        checkedIn: checkedInFilter,
      );
      if (page == 1) guests.clear();
      guests.addAll(result.items);
      total = result.total;
      page++;
    } catch (e) {
      hasError = true;
      error = _friendlyError(e);
    }
    loading = false;
    loadingMore = false;
    notifyListeners();
  }

  List<PlannerRegistrationModel> get filtered {
    if (searchQuery.isEmpty) return guests;
    final q = searchQuery.toLowerCase();
    return guests.where((g) {
      return g.fullName.toLowerCase().contains(q) ||
          g.emailAddress.toLowerCase().contains(q) ||
          (g.organisation?.toLowerCase().contains(q) ?? false);
    }).toList();
  }

  void setSearchQuery(String q) {
    searchQuery = q;
    notifyListeners();
  }
}

// ──────────────────────────────────────────────────────
// PLANNER — Registration form provider (AsyncNotifier)
// ──────────────────────────────────────────────────────

final plannerFormProvider = FutureProvider.autoDispose
    .family<PlannerFormModel?, String>((ref, eventId) async {
  try {
    return await ref.read(guestManagementApiProvider).getForm(eventId);
  } catch (_) {
    return null;
  }
});

// ──────────────────────────────────────────────────────
// PUBLIC — Public form provider (FutureProvider)
// ──────────────────────────────────────────────────────

final publicFormProvider = FutureProvider.autoDispose
    .family<PublicFormModel, String>((ref, publicId) async {
  return ref.read(guestManagementApiProvider).getPublicForm(publicId);
});

// ──────────────────────────────────────────────────────
// Upload result state (simple ChangeNotifier)
// ──────────────────────────────────────────────────────

final uploadControllerProvider = ChangeNotifierProvider.autoDispose
    .family<UploadController, String>((ref, eventId) {
  return UploadController(ref.read(guestManagementApiProvider), eventId);
});

enum UploadState { idle, uploading, success, error }

class UploadController extends ChangeNotifier {
  final GuestManagementRemoteDataSource api;
  final String eventId;

  UploadState state = UploadState.idle;
  BulkUploadResult? result;
  String? error;
  String? selectedFileName;
  int? selectedFileSize;

  UploadController(this.api, this.eventId);

  Future<void> upload(List<int> fileBytes, String fileName) async {
    state = UploadState.uploading;
    error = null;
    result = null;
    notifyListeners();
    try {
      result = await api.uploadGuestList(eventId, fileBytes, fileName);
      state = UploadState.success;
    } catch (e) {
      error = _friendlyError(e);
      state = UploadState.error;
    }
    notifyListeners();
  }

  void reset() {
    state = UploadState.idle;
    result = null;
    error = null;
    selectedFileName = null;
    selectedFileSize = null;
    notifyListeners();
  }
}

// ──────────────────────────────────────────────────────
// Check-in controller
// ──────────────────────────────────────────────────────

final checkInControllerProvider = ChangeNotifierProvider.autoDispose
    .family<CheckInController, String>((ref, eventId) {
  return CheckInController(ref.read(guestManagementApiProvider), eventId);
});

enum CheckInState { idle, loading, success, error }

class CheckInController extends ChangeNotifier {
  final GuestManagementRemoteDataSource api;
  final String eventId;

  CheckInState state = CheckInState.idle;
  CheckInResultModel? result;
  String? error;

  CheckInController(this.api, this.eventId);

  Future<void> checkIn(String token) async {
    state = CheckInState.loading;
    error = null;
    result = null;
    notifyListeners();
    try {
      result = await api.checkIn(eventId, token);
      state = CheckInState.success;
    } catch (e) {
      error = _friendlyError(e);
      state = CheckInState.error;
    }
    notifyListeners();
  }

  void reset() {
    state = CheckInState.idle;
    result = null;
    error = null;
    notifyListeners();
  }
}

// ──────────────────────────────────────────────────────
// RSVP controller (public)
// ──────────────────────────────────────────────────────

final rsvpControllerProvider =
    ChangeNotifierProvider.autoDispose<RsvpController>((ref) {
  return RsvpController(ref.read(guestManagementApiProvider));
});

enum RsvpControllerState { idle, loading, success, error }

class RsvpController extends ChangeNotifier {
  final GuestManagementRemoteDataSource api;

  RsvpControllerState state = RsvpControllerState.idle;
  PublicRegistrationModel? result;
  String? error;

  RsvpController(this.api);

  Future<void> submitRsvp(
    String publicReference,
    String secret,
    String response,
  ) async {
    state = RsvpControllerState.loading;
    error = null;
    notifyListeners();
    try {
      result = await api.submitRsvp(publicReference, secret, response);
      state = RsvpControllerState.success;
    } catch (e) {
      error = _friendlyError(e);
      state = RsvpControllerState.error;
    }
    notifyListeners();
  }
}

// ──────────────────────────────────────────────────────
// Shared error helper
// ──────────────────────────────────────────────────────

String _friendlyError(Object e) {
  final msg = e.toString();
  if (msg.contains('SocketException') || msg.contains('connection')) {
    return 'No internet connection. Please check your network.';
  }
  if (msg.contains('401')) {
    return 'Session expired. Please log in again.';
  }
  if (msg.contains('403')) {
    return 'You do not have access to this resource.';
  }
  if (msg.contains('404')) {
    return 'The requested resource was not found.';
  }
  if (msg.contains('409')) {
    return 'A conflict occurred. The guest may already be registered.';
  }
  if (msg.contains('422') || msg.contains('400')) {
    return 'Invalid data. Please check your inputs and try again.';
  }
  if (msg.contains('500') || msg.contains('502') || msg.contains('503')) {
    return 'Server error. Please try again later.';
  }
  // Try to extract a message field from a DioException response
  final jsonMatch = RegExp(r'"(?:message|error)"\s*:\s*"([^"]+)"').firstMatch(msg);
  if (jsonMatch != null) return jsonMatch.group(1)!;
  return 'An unexpected error occurred. Please try again.';
}
