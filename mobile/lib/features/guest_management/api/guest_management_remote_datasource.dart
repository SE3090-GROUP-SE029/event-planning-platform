import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import '../../../core/api/dio_client.dart';
import '../models/guest_registration_model.dart';
import '../models/registration_form_model.dart';

/// Remote data source for all C4 Guest Management API calls.
///
/// Endpoints consumed:
///   GET  /api/events/{eventId}/registration-form
///   POST /api/events/{eventId}/registration-form/upload
///   GET  /api/events/{eventId}/registration-form/registrations
///   GET  /api/events/{eventId}/registration-form/registrations/{id}
///   POST /api/events/{eventId}/registration-form/registrations/{id}/cancel
///   POST /api/events/{eventId}/registration-form/check-in
///   GET  /api/public/registration-forms/{publicId}
///   POST /api/public/registration-forms/{publicId}/registrations
///   POST /api/public/registrations/{publicReference}/status
///   POST /api/public/registrations/{publicReference}/rsvp
class GuestManagementRemoteDataSource {
  static const Duration _uploadTimeout = Duration(seconds: 60);

  final Dio _dio;

  GuestManagementRemoteDataSource({Dio? dio}) : _dio = dio ?? DioClient().dio;

  // ──────────────────────────────────────────
  // PLANNER — Registration Form
  // ──────────────────────────────────────────

  /// Get the registration form for an event (planner-authenticated).
  Future<PlannerFormModel> getForm(String eventId) async {
    final response = await _dio.get(
      '/api/events/$eventId/registration-form',
    );
    return PlannerFormModel.fromJson(response.data as Map<String, dynamic>);
  }

  /// Create a registration form draft for an event.
  Future<PlannerFormModel> createForm(
    String eventId, {
    required DateTime opensAt,
    required DateTime closesAt,
    required int seatLimit,
  }) async {
    final response = await _dio.post(
      '/api/events/$eventId/registration-form',
      data: {
        'opensAt': opensAt.toUtc().toIso8601String(),
        'closesAt': closesAt.toUtc().toIso8601String(),
        'seatLimit': seatLimit,
      },
    );
    return PlannerFormModel.fromJson(response.data as Map<String, dynamic>);
  }

  /// Update the registration form settings.
  Future<PlannerFormModel> updateForm(
    String eventId, {
    required DateTime opensAt,
    required DateTime closesAt,
    required int seatLimit,
  }) async {
    final response = await _dio.put(
      '/api/events/$eventId/registration-form',
      data: {
        'opensAt': opensAt.toUtc().toIso8601String(),
        'closesAt': closesAt.toUtc().toIso8601String(),
        'seatLimit': seatLimit,
      },
    );
    return PlannerFormModel.fromJson(response.data as Map<String, dynamic>);
  }

  /// Publish the event's saved registration form.
  Future<PlannerFormModel> publishForm(String eventId) async {
    final response = await _dio.post(
      '/api/events/$eventId/registration-form/publish',
    );
    return PlannerFormModel.fromJson(response.data as Map<String, dynamic>);
  }

  // ──────────────────────────────────────────
  // PLANNER — Guest List Upload
  // ──────────────────────────────────────────

  /// Upload a CSV/PDF/DOCX file and email registration links to valid guest rows.
  /// Returns queued-email counts, duplicates, and row errors.
  Future<BulkUploadResult> uploadGuestList(
      String eventId, List<int> fileBytes, String fileName,
      {ProgressCallback? onSendProgress}) async {
    final extension = fileName.split('.').last.toLowerCase();
    final contentType = switch (extension) {
      'csv' => DioMediaType('text', 'csv'),
      'pdf' => DioMediaType('application', 'pdf'),
      'docx' => DioMediaType(
          'application',
          'vnd.openxmlformats-officedocument.wordprocessingml.document',
        ),
      _ => throw ArgumentError('Only CSV, PDF, and DOCX files are supported.'),
    };
    if (fileBytes.isEmpty) {
      throw ArgumentError('The selected file is empty.');
    }
    if (fileBytes.length > 5 * 1024 * 1024) {
      throw ArgumentError('File must not exceed 5 MB.');
    }

    final formData = FormData.fromMap({
      'file': MultipartFile.fromBytes(
        fileBytes,
        filename: fileName,
        contentType: contentType,
      ),
    });
    const endpointSuffix = '/registration-form/upload';
    final endpoint = '/api/events/$eventId$endpointSuffix';
    if (kDebugMode) {
      debugPrint(
        '[GuestFileUpload] POST ${_dio.options.baseUrl}$endpoint '
        'multipart/form-data; extension=$extension; bytes=${fileBytes.length}; '
        'authorization=attached',
      );
    }
    final response = await _dio.post(
      endpoint,
      onSendProgress: onSendProgress,
      data: formData,
      options: Options(
        contentType: Headers.multipartFormDataContentType,
        connectTimeout: _uploadTimeout,
        receiveTimeout: _uploadTimeout,
        sendTimeout: _uploadTimeout,
      ),
    );
    if (kDebugMode) {
      debugPrint(
        '[GuestFileUpload] response status=${response.statusCode}',
      );
    }
    return BulkUploadResult.fromJson(response.data as Map<String, dynamic>);
  }

  // ──────────────────────────────────────────
  // PLANNER — Guest List Management
  // ──────────────────────────────────────────

  /// List registrations for an event with optional filters.
  Future<PlannerRegistrationPage> listRegistrations(
    String eventId, {
    int page = 1,
    int pageSize = 50,
    String? status,
    String? rsvpStatus,
    bool? isWaitlisted,
    bool? checkedIn,
  }) async {
    final response = await _dio.get(
      '/api/events/$eventId/registration-form/registrations',
      queryParameters: {
        'page': page,
        'pageSize': pageSize,
        if (status != null) 'status': status,
        if (rsvpStatus != null) 'rsvpStatus': rsvpStatus,
        if (isWaitlisted != null) 'isWaitlisted': isWaitlisted,
        if (checkedIn != null) 'checkedIn': checkedIn,
      },
    );
    return PlannerRegistrationPage.fromJson(
        response.data as Map<String, dynamic>);
  }

  /// Get a single registration by ID.
  Future<PlannerRegistrationModel> getRegistration(
    String eventId,
    int registrationId,
  ) async {
    final response = await _dio.get(
      '/api/events/$eventId/registration-form/registrations/$registrationId',
    );
    return PlannerRegistrationModel.fromJson(
        response.data as Map<String, dynamic>);
  }

  /// Cancel a registration.
  Future<PlannerRegistrationModel> cancelRegistration(
    String eventId,
    int registrationId,
  ) async {
    final response = await _dio.post(
      '/api/events/$eventId/registration-form/registrations/$registrationId/cancel',
    );
    return PlannerRegistrationModel.fromJson(
        response.data as Map<String, dynamic>);
  }

  // ──────────────────────────────────────────
  // PLANNER — QR Check-In
  // ──────────────────────────────────────────

  /// Check in a guest via QR token.
  /// The backend is the source of truth for all validation.
  Future<CheckInResultModel> checkIn(
    String eventId,
    String token,
  ) async {
    final response = await _dio.post(
      '/api/events/$eventId/registration-form/check-in',
      data: {'token': token},
    );
    return CheckInResultModel.fromJson(response.data as Map<String, dynamic>);
  }

  // ──────────────────────────────────────────
  // PUBLIC — Registration Form (No auth)
  // ──────────────────────────────────────────

  /// Get the public registration form by its public ID.
  Future<PublicFormModel> getPublicForm(String publicId) async {
    final response = await _dio.get(
      '/api/public/registration-forms/$publicId',
    );
    return PublicFormModel.fromJson(response.data as Map<String, dynamic>);
  }

  /// Submit a public guest registration.
  Future<PublicRegistrationModel> submitRegistration(
    String publicId, {
    required String fullName,
    required String emailAddress,
    String? organisation,
    String? phoneNumber,
    List<Map<String, dynamic>>? answers,
  }) async {
    final response = await _dio.post(
      '/api/public/registration-forms/$publicId/registrations',
      data: {
        'fullName': fullName,
        'emailAddress': emailAddress,
        if (organisation != null) 'organisation': organisation,
        if (phoneNumber != null) 'phoneNumber': phoneNumber,
        if (answers != null && answers.isNotEmpty) 'answers': answers,
      },
    );
    return PublicRegistrationModel.fromJson(
        response.data as Map<String, dynamic>);
  }

  // ──────────────────────────────────────────
  // PUBLIC — Registration Status & RSVP
  // ──────────────────────────────────────────

  /// Get the current status of a registration using the secret.
  Future<PublicRegistrationModel> getRegistrationStatus(
    String publicReference,
    String secret,
  ) async {
    final response = await _dio.post(
      '/api/public/registrations/$publicReference/status',
      data: {'secret': secret},
    );
    return PublicRegistrationModel.fromJson(
        response.data as Map<String, dynamic>);
  }

  /// Submit an RSVP response.
  /// [response] must be "ACCEPTED", "DECLINED", or "MAYBE".
  Future<PublicRegistrationModel> submitRsvp(
    String publicReference,
    String secret,
    String rsvpResponse,
  ) async {
    final response = await _dio.post(
      '/api/public/registrations/$publicReference/rsvp',
      data: {
        'secret': secret,
        'response': rsvpResponse,
      },
    );
    return PublicRegistrationModel.fromJson(
        response.data as Map<String, dynamic>);
  }
}
