class RefreshRequestModel {
  final String refreshToken;

  const RefreshRequestModel({required this.refreshToken});

  Map<String, dynamic> toJson() => {'refreshToken': refreshToken};
}
