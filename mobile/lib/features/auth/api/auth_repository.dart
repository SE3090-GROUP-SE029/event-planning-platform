import 'auth_remote_datasource.dart';
import '../models/auth_response_model.dart';
import '../models/login_request_model.dart';
import '../models/register_request_model.dart';
import '../models/refresh_request_model.dart';

class AuthRepository {
  final AuthRemoteDataSource remoteDataSource;

  AuthRepository({required this.remoteDataSource});

  Future<AuthResponseModel> login(LoginRequestModel request) {
    return remoteDataSource.login(request);
  }

  Future<AuthResponseModel> register(RegisterRequestModel request) {
    return remoteDataSource.register(request);
  }

  Future<void> logout(String refreshToken) {
    return remoteDataSource.logout(refreshToken);
  }

  Future<AuthResponseModel> refresh(String refreshToken) {
    return remoteDataSource.refresh(
      RefreshRequestModel(refreshToken: refreshToken),
    );
  }
}
