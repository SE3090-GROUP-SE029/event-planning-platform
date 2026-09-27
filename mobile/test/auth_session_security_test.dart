import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/api/session_store.dart';
import 'package:mobile/features/auth/api/auth_remote_datasource.dart';
import 'package:mobile/features/auth/api/auth_repository.dart';
import 'package:mobile/features/auth/models/auth_response_model.dart';
import 'package:mobile/features/auth/models/current_user_model.dart';
import 'package:mobile/features/auth/models/login_request_model.dart';
import 'package:mobile/features/auth/models/refresh_request_model.dart';
import 'package:mobile/features/auth/models/register_request_model.dart';

void main() {
  test('restores an authorized complete session from secure storage', () async {
    final store = _MemorySessionStore()..session = _session();
    final repository = AuthRepository(
      remoteDataSource: _FakeRemoteDataSource(),
      sessionStore: store,
    );

    final restored = await repository.restoreSession();
    expect(restored?.toJson(), store.session?.toJson());
  });

  test('refreshes the persisted profile and role during session restoration',
      () async {
    final store = _MemorySessionStore()..session = _session();
    final repository = AuthRepository(
      remoteDataSource: _FakeRemoteDataSource(
        profile: const CurrentUserModel(
          userId: 'user-1',
          email: 'vendor@test.com',
          roles: ['VENDOR'],
        ),
      ),
      sessionStore: store,
    );

    final restored = await repository.restoreSession();

    expect(restored?.email, 'vendor@test.com');
    expect(restored?.roles, ['VENDOR']);
    expect(store.session?.roles, ['VENDOR']);
  });

  test('clears a stored session with an unsupported role during restore',
      () async {
    final store = _MemorySessionStore()
      ..session = _session(roles: const ['ADMIN']);
    final repository = AuthRepository(
      remoteDataSource: _FakeRemoteDataSource(),
      sessionStore: store,
    );

    expect(await repository.restoreSession(), isNull);
    expect(store.session, isNull);
  });

  test('login persists an accepted mobile role', () async {
    final store = _MemorySessionStore();
    final repository = AuthRepository(
      remoteDataSource: _FakeRemoteDataSource(response: _session()),
      sessionStore: store,
    );

    final result = await repository.login(
      LoginRequestModel(email: 'user@test.com', password: 'pw'),
    );

    expect(result.roles, ['EVENT_PLANNER']);
    expect(store.session?.accessToken, 'access-token');
  });

  test('login rejects and revokes ADMIN without persisting its token',
      () async {
    final store = _MemorySessionStore();
    final remote = _FakeRemoteDataSource(
      response: _session(roles: const ['ADMIN']),
    );
    final repository = AuthRepository(
      remoteDataSource: remote,
      sessionStore: store,
    );

    await expectLater(
      repository.login(
        LoginRequestModel(email: 'admin@test.com', password: 'pw'),
      ),
      throwsA(isA<UnauthorizedMobileRoleException>()),
    );

    expect(remote.revokedRefreshToken, 'refresh-token');
    expect(store.session, isNull);
  });

  test('register rejects unknown roles without persisting its token', () async {
    final store = _MemorySessionStore();
    final repository = AuthRepository(
      remoteDataSource: _FakeRemoteDataSource(
        response: _session(roles: const ['UNKNOWN']),
      ),
      sessionStore: store,
    );

    await expectLater(
      repository.register(
        RegisterRequestModel(
          email: 'unknown@test.com',
          password: 'pw',
          firstName: 'Unknown',
          lastName: 'User',
          role: 'UNKNOWN',
        ),
      ),
      throwsA(isA<UnauthorizedMobileRoleException>()),
    );
    expect(store.session, isNull);
  });

  test('unknown and mixed roles are not accepted for mobile sessions', () {
    expect(_session(roles: const ['UNKNOWN']).isAuthorizedForMobile, isFalse);
    expect(
      _session(roles: const ['EVENT_PLANNER', 'ADMIN']).isAuthorizedForMobile,
      isFalse,
    );
    expect(_session(roles: const ['VENDOR']).isAuthorizedForMobile, isTrue);
  });

  test('logout clears secure storage even when server revocation fails',
      () async {
    final store = _MemorySessionStore()..session = _session();
    final repository = AuthRepository(
      remoteDataSource: _FakeRemoteDataSource(failLogout: true),
      sessionStore: store,
    );

    await expectLater(
      repository.logout('refresh-token'),
      throwsA(isA<LogoutFailureException>()),
    );
    expect(store.session, isNull);
  });

  test('logout clears stored session if the in-memory session is missing',
      () async {
    final store = _MemorySessionStore()..session = _session();
    final repository = AuthRepository(
      remoteDataSource: _FakeRemoteDataSource(),
      sessionStore: store,
    );

    await repository.logout('');

    expect(store.session, isNull);
  });

  test('strictly rejects incomplete authentication responses', () {
    expect(
      () => AuthResponseModel.fromJson({'accessToken': 'access'}),
      throwsA(isA<FormatException>()),
    );
    expect(
      () => AuthResponseModel.fromJson({
        ..._session().toJson(),
        'roles': ['VENDOR', 12],
      }),
      throwsA(isA<FormatException>()),
    );
  });
}

AuthResponseModel _session({List<String> roles = const ['EVENT_PLANNER']}) =>
    AuthResponseModel(
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      accessTokenExpiresAt: '2099-01-01T00:00:00Z',
      userId: 'user-1',
      email: 'user@test.com',
      roles: roles,
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

class _FakeRemoteDataSource implements AuthRemoteDataSource {
  final AuthResponseModel response;
  final bool failLogout;
  final CurrentUserModel profile;
  String? revokedRefreshToken;

  _FakeRemoteDataSource({
    AuthResponseModel? response,
    this.failLogout = false,
    CurrentUserModel? profile,
  })  : response = response ?? _session(),
        profile = profile ??
            const CurrentUserModel(
              userId: 'user-1',
              email: 'user@test.com',
              roles: ['EVENT_PLANNER'],
            );

  @override
  Future<AuthResponseModel> login(LoginRequestModel request) async => response;

  @override
  Future<AuthResponseModel> register(RegisterRequestModel request) async =>
      response;

  @override
  Future<void> logout(String refreshToken) async {
    revokedRefreshToken = refreshToken;
    if (failLogout) throw Exception('network unavailable');
  }

  @override
  Future<AuthResponseModel> refresh(RefreshRequestModel request) async =>
      response;

  @override
  Future<CurrentUserModel> getCurrentUser() async => profile;
}
