import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/auth/api/auth_remote_datasource.dart';
import 'package:mobile/features/auth/api/auth_repository.dart';
import 'package:mobile/features/auth/models/auth_response_model.dart';
import 'package:mobile/features/auth/models/current_user_model.dart';
import 'package:mobile/features/auth/models/login_request_model.dart';
import 'package:mobile/features/auth/models/refresh_request_model.dart';
import 'package:mobile/features/auth/models/register_request_model.dart';
import 'package:mobile/features/auth/pages/login_page.dart';
import 'package:mobile/features/auth/providers/auth_providers.dart';
import 'package:mobile/core/api/session_store.dart';
import 'package:mobile/features/dashboard/pages/dashboard_page.dart';
import 'package:mobile/features/onboarding/pages/onboarding_page.dart';
import 'package:mobile/main.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  testWidgets('successful planner login replaces the login route',
      (tester) async {
    SharedPreferences.setMockInitialValues({'onboarding_completed': true});
    final repository = _TestAuthRepository(_plannerSession());
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          startupConfigurationProvider.overrideWith((ref) async => null),
          authRepositoryProvider.overrideWithValue(repository),
        ],
        child: const MyApp(),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(LoginPage), findsOneWidget);
    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'planner@test.com');
    await tester.enterText(fields.at(1), 'password');
    await tester.tap(find.text('Sign in'));
    await tester.pumpAndSettle();

    expect(find.byType(LoginPage), findsNothing);
    expect(find.byType(DashboardPage), findsOneWidget);
    expect(find.text('Planner'), findsWidgets);
    expect(
      Navigator.of(tester.element(find.byType(DashboardPage))).canPop(),
      isFalse,
    );

    await tester.tap(find.byIcon(Icons.logout_rounded));
    await tester.pumpAndSettle();

    expect(find.byType(DashboardPage), findsNothing);
    expect(find.byType(OnboardingPage), findsOneWidget);
    expect(
      SharedPreferences.getInstance().then(
        (preferences) => preferences.getBool('onboarding_completed'),
      ),
      completion(false),
    );

    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          startupConfigurationProvider.overrideWith((ref) async => null),
          authRepositoryProvider.overrideWithValue(repository),
        ],
        child: const MyApp(),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.byType(OnboardingPage), findsOneWidget);
  });

  testWidgets('first-time users can navigate onboarding and skip to login',
      (tester) async {
    SharedPreferences.setMockInitialValues({'onboarding_completed': false});
    final repository = _TestAuthRepository(_plannerSession());
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          startupConfigurationProvider.overrideWith((ref) async => null),
          authRepositoryProvider.overrideWithValue(repository),
        ],
        child: const MyApp(),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(OnboardingPage), findsOneWidget);
    await tester.tap(find.text('Next'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Previous'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Skip'));
    await tester.pumpAndSettle();

    expect(find.byType(LoginPage), findsOneWidget);
    final preferences = await SharedPreferences.getInstance();
    expect(preferences.getBool('onboarding_completed'), isTrue);
  });

  testWidgets('Get Started completes all onboarding slides', (tester) async {
    SharedPreferences.setMockInitialValues({'onboarding_completed': false});
    final repository = _TestAuthRepository(_plannerSession());
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          startupConfigurationProvider.overrideWith((ref) async => null),
          authRepositoryProvider.overrideWithValue(repository),
        ],
        child: const MyApp(),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Next'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Next'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Get Started'));
    await tester.pumpAndSettle();

    expect(find.byType(LoginPage), findsOneWidget);
    final preferences = await SharedPreferences.getInstance();
    expect(preferences.getBool('onboarding_completed'), isTrue);
  });

  testWidgets('authenticated users bypass onboarding at startup',
      (tester) async {
    SharedPreferences.setMockInitialValues({'onboarding_completed': false});
    final repository = _TestAuthRepository(
      _plannerSession(),
      restoreExistingSession: true,
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          startupConfigurationProvider.overrideWith((ref) async => null),
          authRepositoryProvider.overrideWithValue(repository),
        ],
        child: const MyApp(),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(OnboardingPage), findsNothing);
    expect(find.byType(DashboardPage), findsOneWidget);
  });

  testWidgets('startup splash remains visible while session is loading',
      (tester) async {
    SharedPreferences.setMockInitialValues({'onboarding_completed': true});
    final repository = _PendingRestoreAuthRepository();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          startupConfigurationProvider.overrideWith((ref) async => null),
          authRepositoryProvider.overrideWithValue(repository),
        ],
        child: const MyApp(),
      ),
    );

    expect(find.text('Plan It'), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    expect(find.byType(LoginPage), findsNothing);

    repository.completeRestore(_plannerSession());
    await tester.pumpAndSettle();
    expect(find.byType(DashboardPage), findsOneWidget);
  });

  testWidgets('startup restores an existing session without showing login',
      (tester) async {
    SharedPreferences.setMockInitialValues({'onboarding_completed': true});
    final repository = _TestAuthRepository(
      _plannerSession(),
      restoreExistingSession: true,
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          startupConfigurationProvider.overrideWith((ref) async => null),
          authRepositoryProvider.overrideWithValue(repository),
        ],
        child: const MyApp(),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(LoginPage), findsNothing);
    expect(find.byType(DashboardPage), findsOneWidget);
  });
}

AuthResponseModel _plannerSession() => AuthResponseModel(
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      accessTokenExpiresAt: '2099-01-01T00:00:00Z',
      userId: 'planner-1',
      email: 'planner@test.com',
      roles: const ['EVENT_PLANNER'],
    );

class _TestAuthRepository extends AuthRepository {
  final AuthResponseModel session;
  final bool restoreExistingSession;

  _TestAuthRepository(
    this.session, {
    this.restoreExistingSession = false,
  }) : super(
          remoteDataSource: _UnusedRemoteDataSource(),
          sessionStore: _UnusedSessionStore(),
        );

  @override
  Future<AuthResponseModel?> restoreSession() async =>
      restoreExistingSession ? session : null;

  @override
  Future<AuthResponseModel> login(LoginRequestModel request) async => session;

  @override
  Future<void> logout(String refreshToken) async {}
}

class _PendingRestoreAuthRepository extends _TestAuthRepository {
  final _restore = Completer<AuthResponseModel?>();

  _PendingRestoreAuthRepository() : super(_plannerSession());

  @override
  Future<AuthResponseModel?> restoreSession() => _restore.future;

  void completeRestore(AuthResponseModel session) {
    _restore.complete(session);
  }
}

class _UnusedRemoteDataSource implements AuthRemoteDataSource {
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

class _UnusedSessionStore implements SessionStore {
  @override
  Future<AuthResponseModel?> readSession() async => null;

  @override
  Future<void> writeSession(AuthResponseModel session) async {}

  @override
  Future<void> clearSession() async {}
}
