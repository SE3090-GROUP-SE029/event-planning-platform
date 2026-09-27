class CurrentUserModel {
  final String userId;
  final String email;
  final List<String> roles;

  const CurrentUserModel({
    required this.userId,
    required this.email,
    required this.roles,
  });

  factory CurrentUserModel.fromJson(Map<String, dynamic> json) {
    final rolesValue = json['roles'];
    if (rolesValue is! List<dynamic> ||
        rolesValue.any((role) => role is! String)) {
      throw const FormatException('The current-user response has invalid roles.');
    }

    return CurrentUserModel(
      userId: _requiredString(json, 'userId'),
      email: _requiredString(json, 'email'),
      roles: rolesValue.cast<String>(),
    );
  }

  static String _requiredString(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is! String || value.trim().isEmpty) {
      throw FormatException('The current-user response is missing $key.');
    }
    return value;
  }
}
