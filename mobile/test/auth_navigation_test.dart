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
import 'package:mobile/main.dart';

void main() {
  testWidgets('successful planner login replaces the login route',
      (tester) async {
    final repository = _TestAuthRepository(_plannerSession());
    await tester.pumpWidget(
      ProviderScope(
        overrides: [authRepositoryProvider.overrideWithValue(repository)],
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
    expect(find.byType(LoginPage), findsOneWidget);
  });

  testWidgets('startup restores an existing session without showing login',
      (tester) async {
    final repository = _TestAuthRepository(
      _plannerSession(),
      restoreExistingSession: true,
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [authRepositoryProvider.overrideWithValue(repository)],
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
