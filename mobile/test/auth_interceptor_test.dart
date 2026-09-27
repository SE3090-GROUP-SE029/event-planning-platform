import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/api/dio_client.dart';
import 'package:mobile/core/api/session_store.dart';
import 'package:mobile/features/auth/models/auth_response_model.dart';

void main() {
  test('attaches access token and single-flights concurrent 401 refreshes',
      () async {
    final store = _MemorySessionStore()..session = _session('old-access');
    final refreshStarted = Completer<void>();
    final allowRefresh = Completer<void>();
    var refreshRequests = 0;
    var resourceRequests = 0;
    final adapter = _Adapter((request) async {
      if (request.path == '/api/auth/refresh') {
        refreshRequests++;
        if (!refreshStarted.isCompleted) refreshStarted.complete();
        await allowRefresh.future;
        return _jsonResponse(_session('new-access').toJson());
      }
      resourceRequests++;
      if (request.headers['Authorization'] == 'Bearer old-access') {
        return _jsonResponse({'message': 'expired'}, statusCode: 401);
      }
      return _jsonResponse({'ok': true});
    });
    final dio = DioClient.withDependencies(
      baseUrl: 'https://api.example.test',
      sessionStore: store,
      adapter: adapter,
    ).dio;

    final first = dio.get<dynamic>('/resource');
    final second = dio.get<dynamic>('/resource');
    await refreshStarted.future;
    allowRefresh.complete();
    await Future.wait([first, second]);

    expect(refreshRequests, 1);
    expect(resourceRequests, 4);
    expect(store.session?.accessToken, 'new-access');
  });

  test('failed refresh clears local session and does not recurse', () async {
    final store = _MemorySessionStore()..session = _session('old-access');
    var refreshRequests = 0;
    var resourceRequests = 0;
    var expirationCallbacks = 0;
    final adapter = _Adapter((request) async {
      if (request.path == '/api/auth/refresh') {
        refreshRequests++;
        return _jsonResponse({'error': 'refresh rejected'}, statusCode: 401);
      }
      resourceRequests++;
      return _jsonResponse({'error': 'unauthorized'}, statusCode: 401);
    });
    final client = DioClient.withDependencies(
      baseUrl: 'https://api.example.test',
      sessionStore: store,
      adapter: adapter,
    );
    client.onSessionExpired = () => expirationCallbacks++;

    await expectLater(
      client.dio.get<dynamic>('/resource'),
      throwsA(isA<DioException>()),
    );

    expect(refreshRequests, 1);
    expect(resourceRequests, 1);
    expect(store.session, isNull);
    expect(expirationCallbacks, 1);
  });

  test('auth-route 401 does not invoke refresh', () async {
    final store = _MemorySessionStore()..session = _session('old-access');
    var refreshRequests = 0;
    final adapter = _Adapter((request) async {
      if (request.path == '/api/auth/refresh') refreshRequests++;
      return _jsonResponse({'error': 'unauthorized'}, statusCode: 401);
    });
    final dio = DioClient.withDependencies(
      baseUrl: 'https://api.example.test',
      sessionStore: store,
      adapter: adapter,
    ).dio;

    await expectLater(
      dio.post<dynamic>('/api/auth/login'),
      throwsA(isA<DioException>()),
    );

    expect(refreshRequests, 0);
  });
}

AuthResponseModel _session(String accessToken) => AuthResponseModel(
      accessToken: accessToken,
      refreshToken: 'refresh-token',
      accessTokenExpiresAt: '2099-01-01T00:00:00Z',
      userId: 'user-1',
      email: 'user@test.com',
      roles: const ['EVENT_PLANNER'],
    );

ResponseBody _jsonResponse(
  Object body, {
  int statusCode = 200,
}) =>
    ResponseBody.fromString(
      jsonEncode(body),
      statusCode,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );

class _MemorySessionStore implements SessionStore {
  AuthResponseModel? session;

  @override
  Future<AuthResponseModel?> readSession() async => session;

  @override
  Future<void> writeSession(AuthResponseModel session) async {
    this.session = session;
  }

  @override
  Future<void> clearSession() async {
    session = null;
  }
}

class _Adapter implements HttpClientAdapter {
  final Future<ResponseBody> Function(RequestOptions) responder;

  _Adapter(this.responder);

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) =>
      responder(options);

  @override
  void close({bool force = false}) {}
}
