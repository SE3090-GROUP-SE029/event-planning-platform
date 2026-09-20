import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/auth/api/auth_remote_datasource.dart';
import 'package:mobile/features/auth/api/auth_repository.dart';
import 'package:mobile/features/auth/models/auth_response_model.dart';
import 'package:mobile/features/auth/models/login_request_model.dart';
import 'package:mobile/features/auth/models/register_request_model.dart';
import 'package:mobile/features/auth/models/refresh_request_model.dart';
import 'package:mobile/features/auth/providers/auth_providers.dart';

void main() {
  test('login exposes AsyncData with the authenticated session', () async {
    final repository = FakeAuthRepository();
    final container = ProviderContainer(
      overrides: [authRepositoryProvider.overrideWithValue(repository)],
    );
    addTearDown(container.dispose);

    await container
        .read(authNotifierProvider.notifier)
        .login('user@test.com', 'password');

    final session = container.read(authNotifierProvider).value;
    expect(session?.email, 'user@test.com');
    expect(container.read(currentUserProvider)?.userId, 'user-1');
  });

  test('login exposes AsyncError when the repository fails', () async {
    final repository = FakeAuthRepository()..shouldFail = true;
    final container = ProviderContainer(
      overrides: [authRepositoryProvider.overrideWithValue(repository)],
    );
    addTearDown(container.dispose);

    await container
        .read(authNotifierProvider.notifier)
        .login('user@test.com', 'password');

    expect(container.read(authNotifierProvider), isA<AsyncError>());
  });

  test('logout clears the current user', () async {
    final repository = FakeAuthRepository();
    final container = ProviderContainer(
      overrides: [authRepositoryProvider.overrideWithValue(repository)],
    );
    addTearDown(container.dispose);
    final notifier = container.read(authNotifierProvider.notifier);

    await notifier.login('user@test.com', 'password');
    await notifier.logout();

    expect(container.read(currentUserProvider), isNull);
    expect(repository.loggedOutToken, 'refresh-token');
  });

  test('refreshToken replaces the current session', () async {
    final repository = FakeAuthRepository();
    final container = ProviderContainer(
      overrides: [authRepositoryProvider.overrideWithValue(repository)],
    );
    addTearDown(container.dispose);
    final notifier = container.read(authNotifierProvider.notifier);

    await notifier.login('user@test.com', 'password');
    await notifier.refreshToken();

    expect(container.read(currentUserProvider)?.email, 'refreshed@test.com');
  });
}

class FakeAuthRepository extends AuthRepository {
  FakeAuthRepository() : super(remoteDataSource: FakeAuthRemoteDataSource());

  bool shouldFail = false;
  String? loggedOutToken;

  AuthResponseModel _session(String email) => AuthResponseModel(
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        accessTokenExpiresAt: '2099-01-01T00:00:00Z',
        userId: 'user-1',
        email: email,
        roles: const ['EVENT_PLANNER'],
      );

  @override
  Future<AuthResponseModel> login(LoginRequestModel request) async {
    if (shouldFail) throw StateError('login failed');
    return _session(request.email);
  }

  @override
  Future<AuthResponseModel> register(RegisterRequestModel request) async {
    if (shouldFail) throw StateError('register failed');
    return _session(request.email);
  }

  @override
  Future<void> logout(String refreshToken) async {
    loggedOutToken = refreshToken;
  }

  @override
  Future<AuthResponseModel> refresh(String refreshToken) async {
    return _session('refreshed@test.com');
  }
}

class FakeAuthRemoteDataSource implements AuthRemoteDataSource {
  @override
  Future<AuthResponseModel> login(LoginRequestModel request) =>
      throw UnimplementedError();

  @override
  Future<AuthResponseModel> register(RegisterRequestModel request) =>
      throw UnimplementedError();

  @override
  Future<void> logout(String refreshToken) => throw UnimplementedError();

  @override
  Future<AuthResponseModel> refresh(RefreshRequestModel request) =>
      throw UnimplementedError();
}
