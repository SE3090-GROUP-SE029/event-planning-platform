import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/api/dio_client.dart';
import '../api/auth_remote_datasource.dart';
import '../api/auth_repository.dart';
import '../models/auth_response_model.dart';
import '../models/login_request_model.dart';
import '../models/register_request_model.dart';

final dioProvider = Provider<Dio>((ref) => DioClient().dio);

final authRemoteDataSourceProvider = Provider<AuthRemoteDataSource>((ref) {
  return AuthRemoteDataSourceImpl(dio: ref.watch(dioProvider));
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(
    remoteDataSource: ref.watch(authRemoteDataSourceProvider),
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
  Future<AuthResponseModel?> build() async => null;

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
    if (session == null) {
      state = const AsyncData(null);
      return;
    }

    state = const AsyncLoading();
    final result = await AsyncValue.guard(
      () => _repository.logout(session.refreshToken),
    );
    state = result.when(
      data: (_) => const AsyncData(null),
      loading: () => const AsyncLoading(),
      error: (error, stackTrace) => AsyncError(error, stackTrace),
    );
  }

  Future<void> refreshToken() async {
    final session = state.value;
    if (session == null) return;

    state = const AsyncLoading();
    state = await AsyncValue.guard(
      () => _repository.refresh(session.refreshToken),
    );
  }
}
