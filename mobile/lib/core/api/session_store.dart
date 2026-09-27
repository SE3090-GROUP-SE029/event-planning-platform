import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../features/auth/models/auth_response_model.dart';

abstract interface class SessionStore {
  Future<AuthResponseModel?> readSession();
  Future<void> writeSession(AuthResponseModel session);
  Future<void> clearSession();
}

class SecureSessionStore implements SessionStore {
  static const _sessionKey = 'auth_session';
  final FlutterSecureStorage _storage;

  SecureSessionStore({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage();

  @override
  Future<AuthResponseModel?> readSession() async {
    final value = await _storage.read(key: _sessionKey);
    if (value == null) return null;

    try {
      final decoded = jsonDecode(value);
      if (decoded is! Map<String, dynamic>) {
        throw const FormatException('Stored session is not an object.');
      }
      return AuthResponseModel.fromJson(decoded);
    } on FormatException {
      await clearSession();
      return null;
    } on TypeError {
      await clearSession();
      return null;
    }
  }

  @override
  Future<void> writeSession(AuthResponseModel session) {
    return _storage.write(key: _sessionKey, value: jsonEncode(session.toJson()));
  }

  @override
  Future<void> clearSession() => _storage.delete(key: _sessionKey);
}
