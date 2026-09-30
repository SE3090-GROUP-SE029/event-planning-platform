// Models for the C4 Guest Management feature.
//
// All fields map exactly to the backend DTOs in:
//   - PlannerFormResponse
//   - PublicFormResponse
//   - PlannerRegistrationResponse
//   - PublicRegistrationResponse
//   - BulkGuestUploadResult

class FormQuestionModel {
  final String id;
  final String question;
  final bool required;

  const FormQuestionModel({
    required this.id,
    required this.question,
    required this.required,
  });

  factory FormQuestionModel.fromJson(Map<String, dynamic> json) =>
      FormQuestionModel(
        id: json['id']?.toString() ?? '',
        question: json['question'] as String? ?? '',
        required: json['required'] as bool? ?? false,
      );
}

class PlannerFormModel {
  final String id;
  final String eventId;
  final DateTime opensAt;
  final DateTime closesAt;
  final int seatLimit;
  final String status;
  final String? publicId;
  final String? publicPath;
  final DateTime? publishedAt;
  final List<FormQuestionModel> questions;

  const PlannerFormModel({
    required this.id,
    required this.eventId,
    required this.opensAt,
    required this.closesAt,
    required this.seatLimit,
    required this.status,
    this.publicId,
    this.publicPath,
    this.publishedAt,
    required this.questions,
  });

  factory PlannerFormModel.fromJson(Map<String, dynamic> json) =>
      PlannerFormModel(
        id: json['id']?.toString() ?? '',
        eventId: json['eventId']?.toString() ?? '',
        opensAt: DateTime.parse(json['opensAt'] as String),
        closesAt: DateTime.parse(json['closesAt'] as String),
        seatLimit: (json['seatLimit'] as num?)?.toInt() ?? 0,
        status: json['status'] as String? ?? '',
        publicId: json['publicId'] as String?,
        publicPath: json['publicPath'] as String?,
        publishedAt: json['publishedAt'] == null
            ? null
            : DateTime.parse(json['publishedAt'] as String),
        questions: (json['questions'] as List<dynamic>? ?? [])
            .map((q) =>
                FormQuestionModel.fromJson(q as Map<String, dynamic>))
            .toList(),
      );
}

class PublicFormModel {
  final String publicId;
  final String eventName;
  final String? description;
  final DateTime startsAt;
  final DateTime endsAt;
  final String? location;
  final DateTime opensAt;
  final DateTime closesAt;
  final int seatLimit;
  final bool isOpen;
  final List<String> requiredFields;
  final List<String> optionalFields;
  final List<FormQuestionModel> questions;

  const PublicFormModel({
    required this.publicId,
    required this.eventName,
    this.description,
    required this.startsAt,
    required this.endsAt,
    this.location,
    required this.opensAt,
    required this.closesAt,
    required this.seatLimit,
    required this.isOpen,
    required this.requiredFields,
    required this.optionalFields,
    required this.questions,
  });

  factory PublicFormModel.fromJson(Map<String, dynamic> json) =>
      PublicFormModel(
        publicId: json['publicId'] as String? ?? '',
        eventName: json['eventName'] as String? ?? '',
        description: json['description'] as String?,
        startsAt: DateTime.parse(json['startsAt'] as String),
        endsAt: DateTime.parse(json['endsAt'] as String),
        location: json['location'] as String?,
        opensAt: DateTime.parse(json['opensAt'] as String),
        closesAt: DateTime.parse(json['closesAt'] as String),
        seatLimit: (json['seatLimit'] as num?)?.toInt() ?? 0,
        isOpen: json['isOpen'] as bool? ?? false,
        requiredFields: (json['requiredFields'] as List<dynamic>? ?? [])
            .map((f) => f.toString())
            .toList(),
        optionalFields: (json['optionalFields'] as List<dynamic>? ?? [])
            .map((f) => f.toString())
            .toList(),
        questions: (json['questions'] as List<dynamic>? ?? [])
            .map((q) =>
                FormQuestionModel.fromJson(q as Map<String, dynamic>))
            .toList(),
      );
}
