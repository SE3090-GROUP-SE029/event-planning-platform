import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/auth/api/auth_remote_datasource.dart';
import 'package:mobile/features/auth/api/auth_repository.dart';
import 'package:mobile/features/auth/models/auth_response_model.dart';
import 'package:mobile/features/auth/models/current_user_model.dart';
import 'package:mobile/features/auth/models/login_request_model.dart';
import 'package:mobile/features/auth/models/register_request_model.dart';
import 'package:mobile/features/auth/models/refresh_request_model.dart';
import 'package:mobile/features/auth/providers/auth_providers.dart';
import 'package:mobile/core/api/session_store.dart';
import 'package:mobile/features/onboarding/providers/onboarding_provider.dart';

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
    final onboardingStore = MemoryOnboardingStore()..complete = true;
    final container = ProviderContainer(
      overrides: [
        authRepositoryProvider.overrideWithValue(repository),
        onboardingStoreProvider.overrideWithValue(onboardingStore),
      ],
    );
    addTearDown(container.dispose);
    await container.read(onboardingCompletionProvider.future);
    final notifier = container.read(authNotifierProvider.notifier);

    await notifier.login('user@test.com', 'password');
    await notifier.logout();

    expect(container.read(currentUserProvider), isNull);
    expect(repository.loggedOutToken, 'refresh-token');
    expect(onboardingStore.complete, isFalse);
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
  FakeAuthRepository()
      : super(
          remoteDataSource: FakeAuthRemoteDataSource(),
          sessionStore: MemorySessionStore(),
        );

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

  @override
  Future<CurrentUserModel> getCurrentUser() => throw UnimplementedError();
}

class MemorySessionStore implements SessionStore {
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

class MemoryOnboardingStore implements OnboardingStore {
  bool complete = false;

  @override
  Future<bool> isComplete() async => complete;

  @override
  Future<void> setComplete(bool value) async {
    complete = value;
  }
}
