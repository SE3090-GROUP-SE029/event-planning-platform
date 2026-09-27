class PingResponse {
  final String message;
  final String timestamp;

  PingResponse({
    required this.message,
    required this.timestamp,
  });

  factory PingResponse.fromJson(Map<String, dynamic> json) {
    return PingResponse(
      message: json['message'] as String,
      timestamp: json['timestamp'] as String,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'message': message,
      'timestamp': timestamp,
    };
  }
}

class TestMessage {
  final int id;
  final String message;
  final String createdAt;

  TestMessage({
    required this.id,
    required this.message,
    required this.createdAt,
  });

  factory TestMessage.fromJson(Map<String, dynamic> json) {
    return TestMessage(
      id: json['id'] as int,
      message: json['message'] as String,
      createdAt: json['createdAt'] as String,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'message': message,
      'createdAt': createdAt,
    };
  }
}

class CreateTestMessageRequest {
  final String message;

  CreateTestMessageRequest({required this.message});

  Map<String, dynamic> toJson() {
    return {'message': message};
  }
}
