import 'package:dio/dio.dart';
import '../../../core/api/dio_client.dart';
import '../models/auth_response_model.dart';
import '../models/current_user_model.dart';
import '../models/login_request_model.dart';
import '../models/register_request_model.dart';
import '../models/refresh_request_model.dart';

abstract class AuthRemoteDataSource {
  Future<AuthResponseModel> login(LoginRequestModel request);
  Future<AuthResponseModel> register(RegisterRequestModel request);
  Future<void> logout(String refreshToken);
  Future<AuthResponseModel> refresh(RefreshRequestModel request);
  Future<CurrentUserModel> getCurrentUser();
}

class AuthApiException implements Exception {
  final int? statusCode;
  final String message;

  const AuthApiException(this.message, {this.statusCode});

  @override
  String toString() => message;
}

class AuthRemoteDataSourceImpl implements AuthRemoteDataSource {
  final Dio dio;

  AuthRemoteDataSourceImpl({Dio? dio}) : dio = dio ?? DioClient().dio;

  @override
  Future<AuthResponseModel> login(LoginRequestModel request) async {
    try {
      final response = await dio.post(
        '/api/auth/login',
        data: request.toJson(),
      );
      return AuthResponseModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  @override
  Future<AuthResponseModel> register(RegisterRequestModel request) async {
    try {
      final response = await dio.post(
        '/api/auth/register',
        data: request.toJson(),
      );
      return AuthResponseModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  @override
  Future<void> logout(String refreshToken) async {
    try {
      await dio.post(
        '/api/auth/logout',
        data: {'refreshToken': refreshToken},
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  @override
  Future<AuthResponseModel> refresh(RefreshRequestModel request) async {
    try {
      final response = await dio.post(
        '/api/auth/refresh',
        data: request.toJson(),
      );
      return AuthResponseModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  @override
  Future<CurrentUserModel> getCurrentUser() async {
    try {
      final response = await dio.get('/api/auth/me');
      final data = response.data;
      if (data is! Map<String, dynamic>) {
        throw const FormatException(
          'The current-user response has an invalid format.',
        );
      }
      return CurrentUserModel.fromJson(data);
    } on DioException catch (error) {
      throw _handleError(error);
    }
  }

  AuthApiException _handleError(DioException error) {
    if (error.response?.data is Map<String, dynamic>) {
      final data = error.response!.data as Map<String, dynamic>;
      if (data.containsKey('message')) {
        return AuthApiException(
          data['message'].toString(),
          statusCode: error.response?.statusCode,
        );
      }
      if (data.containsKey('error')) {
        return AuthApiException(
          data['error'].toString(),
          statusCode: error.response?.statusCode,
        );
      }
    }
    if (error.response?.statusCode == 401) {
      return AuthApiException(
        'Invalid email or password',
        statusCode: error.response?.statusCode,
      );
    } else if (error.response?.statusCode == 400) {
      return AuthApiException(
        'Invalid request data. Please check your inputs.',
        statusCode: error.response?.statusCode,
      );
    }
    return AuthApiException(
      error.message ?? 'An error occurred. Please try again.',
      statusCode: error.response?.statusCode,
    );
  }
}
