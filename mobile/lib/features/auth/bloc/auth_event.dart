abstract class AuthEvent {}

class LoginEvent extends AuthEvent {
  final String email;
  final String password;

  LoginEvent({
    required this.email,
    required this.password,
  });
}

class RegisterEvent extends AuthEvent {
  final String email;
  final String password;
  final String firstName;
  final String lastName;
  final String role; // 'EVENT_PLANNER' or 'VENDOR'

  RegisterEvent({
    required this.email,
    required this.password,
    required this.firstName,
    required this.lastName,
    required this.role,
  });
}

class LogoutEvent extends AuthEvent {
  final String refreshToken;

  LogoutEvent({required this.refreshToken});
}

class CheckAuthStatusEvent extends AuthEvent {}
