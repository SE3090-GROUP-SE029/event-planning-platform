import '../models/auth_response_model.dart';

abstract class AuthState {}

class AuthInitial extends AuthState {}

class AuthLoading extends AuthState {}

class AuthSuccess extends AuthState {
  final AuthResponseModel authResponse;

  AuthSuccess({required this.authResponse});
}

class AuthFailure extends AuthState {
  final String message;

  AuthFailure({required this.message});
}

class AuthLoggedOut extends AuthState {}
