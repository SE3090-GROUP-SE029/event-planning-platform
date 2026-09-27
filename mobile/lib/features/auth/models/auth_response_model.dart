class AuthResponseModel {
  static const supportedMobileRoles = {'EVENT_PLANNER', 'VENDOR'};

  final String accessToken;
  final String refreshToken;
  final String accessTokenExpiresAt;
  final String userId;
  final String email;
  final List<String> roles;

  AuthResponseModel({
    required this.accessToken,
    required this.refreshToken,
    required this.accessTokenExpiresAt,
    required this.userId,
    required this.email,
    required this.roles,
  });

  factory AuthResponseModel.fromJson(Map<String, dynamic> json) {
    final rolesValue = json['roles'];
    if (rolesValue is! List<dynamic> ||
        rolesValue.any((role) => role is! String)) {
      throw const FormatException('The authentication response has invalid roles.');
    }

    final accessToken = _requiredString(json, 'accessToken');
    final refreshToken = _requiredString(json, 'refreshToken');
    final expiresAt = _requiredString(json, 'accessTokenExpiresAt');
    if (DateTime.tryParse(expiresAt) == null) {
      throw const FormatException(
        'The authentication response has an invalid token expiry.',
      );
    }

    return AuthResponseModel(
      accessToken: accessToken,
      refreshToken: refreshToken,
      accessTokenExpiresAt: expiresAt,
      userId: _requiredString(json, 'userId'),
      email: _requiredString(json, 'email'),
      roles: rolesValue.cast<String>(),
    );
  }

  bool get isAuthorizedForMobile =>
      roles.isNotEmpty && roles.every(supportedMobileRoles.contains);

  AuthResponseModel copyWith({
    String? userId,
    String? email,
    List<String>? roles,
  }) =>
      AuthResponseModel(
        accessToken: accessToken,
        refreshToken: refreshToken,
        accessTokenExpiresAt: accessTokenExpiresAt,
        userId: userId ?? this.userId,
        email: email ?? this.email,
        roles: roles ?? this.roles,
      );

  Map<String, dynamic> toJson() => {
        'accessToken': accessToken,
        'refreshToken': refreshToken,
        'accessTokenExpiresAt': accessTokenExpiresAt,
        'userId': userId,
        'email': email,
        'roles': roles,
      };

  static String _requiredString(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is! String || value.trim().isEmpty) {
      throw FormatException('The authentication response is missing $key.');
    }
    return value;
  }
}
