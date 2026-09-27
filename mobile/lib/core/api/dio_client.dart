import 'package:dio/dio.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

import '../../features/auth/models/auth_response_model.dart';
import 'session_store.dart';

class DioClient {
  static final DioClient _instance = DioClient._fromEnvironment();

  late final Dio _dio;
  final String _baseUrl;
  final SessionStore _sessionStore;
  Future<AuthResponseModel?>? _refreshFuture;
  void Function()? onSessionExpired;

  factory DioClient() => _instance;

  DioClient._fromEnvironment()
      : _baseUrl = validateBaseUrl(dotenv.env['API_BASE_URL']),
        _sessionStore = SecureSessionStore() {
    _configure();
  }

  DioClient.withDependencies({
    required String baseUrl,
    required SessionStore sessionStore,
    HttpClientAdapter? adapter,
  })  : _baseUrl = validateBaseUrl(baseUrl),
        _sessionStore = sessionStore {
    _configure(adapter: adapter);
  }

  Dio get dio => _dio;

  static String validateBaseUrl(String? value) {
    final candidate = value?.trim() ?? '';
    final uri = Uri.tryParse(candidate);
    if (uri == null ||
        !uri.hasScheme ||
        !{'http', 'https'}.contains(uri.scheme.toLowerCase()) ||
        uri.host.isEmpty ||
        uri.userInfo.isNotEmpty) {
      throw const FormatException(
        'API_BASE_URL is missing or invalid. Set it to the backend URL, '
        'for example http://10.0.2.2:5207 on the Android emulator.',
      );
    }
    return candidate;
  }

  void _configure({HttpClientAdapter? adapter}) {
    final options = BaseOptions(
      baseUrl: _baseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 10),
      headers: {'Content-Type': 'application/json'},
    );
    _dio = Dio(options);
    if (adapter != null) _dio.httpClientAdapter = adapter;

    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          if (!_isAuthRoute(options.path)) {
            try {
              final session = await _sessionStore.readSession();
              if (session != null && session.accessToken.isNotEmpty) {
                options.headers['Authorization'] =
                    'Bearer ${session.accessToken}';
              }
            } catch (error) {
              handler.reject(
                DioException(
                  requestOptions: options,
                  error: error,
                  message: 'Unable to read the saved authentication session.',
                ),
              );
              return;
            }
          }
          handler.next(options);
        },
        onError: (error, handler) async {
          final request = error.requestOptions;
          if (error.response?.statusCode != 401 ||
              _isAuthRoute(request.path) ||
              request.extra['authRetry'] == true) {
            handler.next(error);
            return;
          }

          final refresh = _refreshFuture ??= _refreshSession();
          AuthResponseModel? session;
          try {
            session = await refresh;
          } catch (_) {
            session = null;
          } finally {
            if (identical(_refreshFuture, refresh)) _refreshFuture = null;
          }

          if (session == null) {
            await _clearSession();
            try {
              onSessionExpired?.call();
            } catch (_) {
              // Session cleanup must not mask the original unauthorized response.
            }
            handler.next(error);
            return;
          }

          request
            ..extra['authRetry'] = true
            ..headers['Authorization'] = 'Bearer ${session.accessToken}';
          try {
            handler.resolve(await _dio.fetch<dynamic>(request));
          } on DioException catch (retryError) {
            handler.next(retryError);
          } catch (retryError) {
            handler.next(
              DioException(requestOptions: request, error: retryError),
            );
          }
        },
      ),
    );
  }

  Future<AuthResponseModel?> _refreshSession() async {
    try {
      final previous = await _sessionStore.readSession();
      if (previous == null || previous.refreshToken.isEmpty) return null;

      final refreshDio = Dio(
        BaseOptions(
          baseUrl: _baseUrl,
          connectTimeout: _dio.options.connectTimeout,
          receiveTimeout: _dio.options.receiveTimeout,
          headers: {'Content-Type': 'application/json'},
        ),
      );
      refreshDio.httpClientAdapter = _dio.httpClientAdapter;
      final response = await refreshDio.post<dynamic>(
        '/api/auth/refresh',
        data: {'refreshToken': previous.refreshToken},
      );
      if (response.data is! Map<String, dynamic>) return null;
      final session =
          AuthResponseModel.fromJson(response.data as Map<String, dynamic>);
      if (!session.isAuthorizedForMobile) return null;
      await _sessionStore.writeSession(session);
      return session;
    } catch (_) {
      return null;
    }
  }

  Future<void> _clearSession() async {
    try {
      await _sessionStore.clearSession();
    } catch (_) {
      // Keep the request failure as the reported error.
    }
  }

  bool _isAuthRoute(String path) {
    final normalizedPath = Uri.tryParse(path)?.path ?? path;
    return normalizedPath == '/api/auth/login' ||
        normalizedPath == '/api/auth/register' ||
        normalizedPath == '/api/auth/refresh' ||
        normalizedPath == '/api/auth/logout';
  }
}
