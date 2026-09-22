class RegisterRequestModel {
  final String email;
  final String password;
  final String firstName;
  final String lastName;
  final String role; // 'EVENT_PLANNER' or 'VENDOR'

  RegisterRequestModel({
    required this.email,
    required this.password,
    required this.firstName,
    required this.lastName,
    required this.role,
  });

  Map<String, dynamic> toJson() => {
    'email': email,
    'password': password,
    'firstName': firstName,
    'lastName': lastName,
    'role': role,
  };
}
