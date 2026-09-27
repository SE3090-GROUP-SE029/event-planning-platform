import '../../../core/api/session_store.dart';
import 'auth_remote_datasource.dart';
import '../models/auth_response_model.dart';
import '../models/login_request_model.dart';
import '../models/register_request_model.dart';
import '../models/refresh_request_model.dart';

class AuthRepository {
  final AuthRemoteDataSource remoteDataSource;
  final SessionStore sessionStore;

  AuthRepository({
    required this.remoteDataSource,
    SessionStore? sessionStore,
  }) : sessionStore = sessionStore ?? SecureSessionStore();

  Future<AuthResponseModel?> restoreSession() async {
    final session = await sessionStore.readSession();
    if (session == null) return null;
    if (!session.isAuthorizedForMobile) {
      await sessionStore.clearSession();
      return null;
    }

    try {
      final profile = await remoteDataSource.getCurrentUser();
      final restoredSession = session.copyWith(
        userId: profile.userId,
        email: profile.email,
        roles: profile.roles,
      );
      if (!restoredSession.isAuthorizedForMobile) {
        await sessionStore.clearSession();
        return null;
      }
      await sessionStore.writeSession(restoredSession);
      return restoredSession;
    } on AuthApiException catch (error) {
      if (error.statusCode == 401) {
        await sessionStore.clearSession();
        return null;
      }
      rethrow;
    }
  }

  Future<AuthResponseModel> login(LoginRequestModel request) async {
    return _authorizeAndSave(() => remoteDataSource.login(request));
  }

  Future<AuthResponseModel> register(RegisterRequestModel request) async {
    return _authorizeAndSave(() => remoteDataSource.register(request));
  }

  Future<void> logout(String refreshToken) async {
    Object? remoteError;
    if (refreshToken.trim().isNotEmpty) {
      try {
        await remoteDataSource.logout(refreshToken);
      } catch (error) {
        remoteError = error;
      }
    }

    await sessionStore.clearSession();
    if (remoteError != null) {
      throw LogoutFailureException(remoteError);
    }
  }

  Future<AuthResponseModel> refresh(String refreshToken) async {
    try {
      return await _authorizeAndSave(
        () => remoteDataSource.refresh(
          RefreshRequestModel(refreshToken: refreshToken),
        ),
      );
    } catch (_) {
      await sessionStore.clearSession();
      rethrow;
    }
  }

  Future<AuthResponseModel> _authorizeAndSave(
    Future<AuthResponseModel> Function() request,
  ) async {
    final session = await request();
    if (!session.isAuthorizedForMobile) {
      try {
        if (session.refreshToken.isNotEmpty) {
          await remoteDataSource.logout(session.refreshToken);
        }
      } catch (_) {
        // Reject the session even when revocation is unavailable.
      } finally {
        await sessionStore.clearSession();
      }
      throw const UnauthorizedMobileRoleException();
    }
    await sessionStore.writeSession(session);
    return session;
  }
}

class UnauthorizedMobileRoleException implements Exception {
  const UnauthorizedMobileRoleException();

  @override
  String toString() =>
      'This account is not authorized to use the mobile app. '
      'Only EVENT_PLANNER and VENDOR accounts can sign in.';
}

class LogoutFailureException implements Exception {
  final Object cause;

  const LogoutFailureException(this.cause);

  @override
  String toString() =>
      'You have been signed out on this device, but the server could not '
      'revoke the session. Please try again when you are online.';
}
