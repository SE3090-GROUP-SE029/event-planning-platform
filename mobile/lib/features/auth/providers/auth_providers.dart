import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/api/session_store.dart';
import '../../../core/api/dio_client.dart';
import '../api/auth_remote_datasource.dart';
import '../api/auth_repository.dart';
import '../models/auth_response_model.dart';
import '../models/login_request_model.dart';
import '../models/register_request_model.dart';
import '../../onboarding/providers/onboarding_provider.dart';

final sessionStoreProvider = Provider<SessionStore>((ref) {
  return SecureSessionStore();
});

final dioProvider = Provider<Dio>((ref) {
  final client = DioClient();
  client.onSessionExpired = () {
    ref.read(authNotifierProvider.notifier).expireSession();
  };
  ref.onDispose(() => client.onSessionExpired = null);
  return client.dio;
});

final authRemoteDataSourceProvider = Provider<AuthRemoteDataSource>((ref) {
  return AuthRemoteDataSourceImpl(dio: ref.watch(dioProvider));
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(
    remoteDataSource: ref.watch(authRemoteDataSourceProvider),
    sessionStore: ref.watch(sessionStoreProvider),
  );
});

final authNotifierProvider =
    AsyncNotifierProvider<AuthNotifier, AuthResponseModel?>(AuthNotifier.new);

final currentUserProvider = Provider<AuthResponseModel?>((ref) {
  return ref.watch(authNotifierProvider).value;
});

class AuthNotifier extends AsyncNotifier<AuthResponseModel?> {
  AuthRepository get _repository => ref.read(authRepositoryProvider);

  @override
  Future<AuthResponseModel?> build() => _repository.restoreSession();

  Future<void> login(String email, String password) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() {
      return _repository.login(
        LoginRequestModel(email: email, password: password),
      );
    });
  }

  Future<void> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
    required String role,
  }) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() {
      return _repository.register(
        RegisterRequestModel(
          email: email,
          password: password,
          firstName: firstName,
          lastName: lastName,
          role: role,
        ),
      );
    });
  }

  Future<void> logout() async {
    final session = state.value;
    state = const AsyncLoading();
    Object? logoutError;
    StackTrace? logoutStackTrace;
    try {
      await _repository.logout(session?.refreshToken ?? '');
    } catch (error, stackTrace) {
      logoutError = error;
      logoutStackTrace = stackTrace;
    }

    try {
      await ref.read(onboardingCompletionProvider.notifier).reset();
    } catch (error, stackTrace) {
      state = AsyncError(error, stackTrace);
      return;
    }

    if (logoutError != null) {
      state = AsyncError(logoutError, logoutStackTrace!);
    } else {
      state = const AsyncData(null);
    }
  }

  void expireSession() {
    state = const AsyncData(null);
  }

  Future<void> refreshToken() async {
    final session = state.value;
    if (session == null) return;

    state = const AsyncLoading();
    try {
      state = AsyncData(await _repository.refresh(session.refreshToken));
    } catch (error, stackTrace) {
      state = AsyncError(error, stackTrace);
    }
  }
}
