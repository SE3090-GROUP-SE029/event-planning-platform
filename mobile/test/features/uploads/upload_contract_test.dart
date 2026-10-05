import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/api/dio_client.dart';
import 'package:mobile/core/api/session_store.dart';
import 'package:mobile/features/auth/models/auth_response_model.dart';
import 'package:mobile/features/guest_management/api/guest_management_remote_datasource.dart';
import 'package:mobile/features/guest_management/models/guest_registration_model.dart';
import 'package:mobile/features/vendors/api/vendor_remote_datasource.dart';

void main() {
  test('guest upload result reads structured upload response fields', () {
    final result = BulkUploadResult.fromJson({
      'success': true,
      'totalRows': 6,
      'successfulRows': 2,
      'failedRows': 2,
      'duplicateRows': 1,
      'alreadyRegisteredRows': 1,
      'uploadedGuests': 3,
      'queuedEmails': 2,
      'invalidRows': 1,
      'duplicates': 2,
      'deliveryFailedRows': 1,
      'errors': <dynamic>[],
    });

    expect(result.uploadedGuests, 3);
    expect(result.queuedEmails, 2);
    expect(result.invalidRows, 1);
    expect(result.duplicateRows, 2);
    expect(result.deliveryFailedRows, 1);
  });

  group('vendor image upload request', () {
    late RequestOptions? capturedRequest;
    late Dio dio;

    setUp(() {
      capturedRequest = null;
      dio = Dio(BaseOptions(baseUrl: 'https://api.example.test'))
        ..httpClientAdapter = _CaptureAdapter((request) async {
          capturedRequest = request;
          return _jsonResponse({});
        });
    });

    test('sends profile images as authenticated multipart uploads', () async {
      final source = VendorRemoteDataSource(dio: dio);

      await source.uploadProfileImage(
        'test-token',
        _validJpegBytes,
        'logo.jpg',
      );

      expect(capturedRequest?.path, '/api/vendors/me/profile-image');
      expect(capturedRequest?.headers['Authorization'], isNotNull);
      final formData = capturedRequest?.data as FormData;
      expect(formData.files.map((entry) => entry.key), contains('file'));
      expect(formData.files.single.value.filename, 'logo.jpg');
      expect(
        formData.files.single.value.contentType.toString(),
        'image/jpeg',
      );
      final contentType = capturedRequest?.headers.entries
          .firstWhere((entry) => entry.key.toLowerCase() == 'content-type')
          .value
          .toString();
      expect(contentType, contains('multipart/form-data'));
      expect(contentType, contains('boundary='));
    });

    test('rejects oversize and unsupported files before sending', () async {
      final source = VendorRemoteDataSource(dio: dio);

      await expectLater(
        source.uploadGalleryImage(
          'test-token',
          List<int>.filled(VendorRemoteDataSource.maxImageBytes + 1, 0),
          'gallery.png',
        ),
        throwsA(isA<ArgumentError>()),
      );
      await expectLater(
        source.uploadProfileImage('test-token', _validJpegBytes, 'image.gif'),
        throwsA(isA<ArgumentError>()),
      );
      expect(capturedRequest, isNull);
    });
  });

  test('guest upload uses the documented multipart route and file field',
      () async {
    RequestOptions? capturedRequest;
    final client = DioClient.withDependencies(
      baseUrl: 'https://api.example.test',
      sessionStore: _MemorySessionStore(
        AuthResponseModel(
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          accessTokenExpiresAt: '2099-01-01T00:00:00Z',
          userId: 'planner-id',
          email: 'planner@example.test',
          roles: const ['EVENT_PLANNER'],
        ),
      ),
      adapter: _CaptureAdapter((request) async {
        capturedRequest = request;
        return _jsonResponse({
          'totalRows': 1,
          'successfulRows': 1,
          'errors': <dynamic>[],
        });
      }),
    );

    await GuestManagementRemoteDataSource(dio: client.dio).uploadGuestList(
      'event-guid',
      utf8.encode('fullName,emailAddress\nTest Guest,test@example.test'),
      'guests.csv',
    );

    expect(
      capturedRequest?.path,
      '/api/events/event-guid/registration-form/upload',
    );
    expect(capturedRequest?.headers['Authorization'], isNotNull);
    final formData = capturedRequest?.data as FormData;
    expect(formData.files.map((entry) => entry.key), contains('file'));
    expect(formData.files.single.value.filename, 'guests.csv');
    final contentType = capturedRequest?.headers.entries
        .firstWhere((entry) => entry.key.toLowerCase() == 'content-type')
        .value
        .toString();
    expect(contentType, contains('multipart/form-data'));
    expect(contentType, contains('boundary='));
  });
}

const _validJpegBytes = <int>[0xFF, 0xD8, 0xFF, 0x00];

ResponseBody _jsonResponse(Object body) => ResponseBody.fromString(
      jsonEncode(body),
      200,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );

class _CaptureAdapter implements HttpClientAdapter {
  final Future<ResponseBody> Function(RequestOptions) respond;

  _CaptureAdapter(this.respond);

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) =>
      respond(options);

  @override
  void close({bool force = false}) {}
}

class _MemorySessionStore implements SessionStore {
  final AuthResponseModel session;

  _MemorySessionStore(this.session);

  @override
  Future<void> clearSession() async {}

  @override
  Future<AuthResponseModel?> readSession() async => session;

  @override
  Future<void> writeSession(AuthResponseModel session) async {}
}
