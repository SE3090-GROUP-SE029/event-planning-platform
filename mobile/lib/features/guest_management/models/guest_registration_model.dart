// Guest registration models for C4.
//
// Maps to backend:
//   - PlannerRegistrationResponse
//   - PublicRegistrationResponse
//   - BulkGuestUploadResult / GuestUploadRowError

/// Registration status values returned by the backend.
/// Backend enum: CONFIRMED, WAITING_LIST, CANCELLED, PENDING_AI, REJECTED
enum RegistrationStatus {
  confirmed,
  waitingList,
  cancelled,
  pendingAi,
  rejected,
  unknown,
}

/// RSVP status values returned by the backend.
/// Backend enum: NOT_RESPONDED, ACCEPTED, DECLINED, MAYBE, ATTENDED
enum RsvpStatus {
  notResponded,
  accepted,
  declined,
  maybe,
  attended,
  unknown,
}

RegistrationStatus _parseRegistrationStatus(String? value) {
  switch (value?.toUpperCase()) {
    case 'CONFIRMED':
      return RegistrationStatus.confirmed;
    case 'WAITING_LIST':
      return RegistrationStatus.waitingList;
    case 'CANCELLED':
      return RegistrationStatus.cancelled;
    case 'PENDING_AI':
      return RegistrationStatus.pendingAi;
    case 'REJECTED':
      return RegistrationStatus.rejected;
    default:
      return RegistrationStatus.unknown;
  }
}

RsvpStatus _parseRsvpStatus(String? value) {
  switch (value?.toUpperCase()) {
    case 'NOT_RESPONDED':
      return RsvpStatus.notResponded;
    case 'ACCEPTED':
      return RsvpStatus.accepted;
    case 'DECLINED':
      return RsvpStatus.declined;
    case 'MAYBE':
      return RsvpStatus.maybe;
    case 'ATTENDED':
      return RsvpStatus.attended;
    default:
      return RsvpStatus.unknown;
  }
}

class RegistrationAnswerModel {
  final String questionId;
  final String answer;

  const RegistrationAnswerModel({
    required this.questionId,
    required this.answer,
  });

  factory RegistrationAnswerModel.fromJson(Map<String, dynamic> json) =>
      RegistrationAnswerModel(
        questionId: json['questionId']?.toString() ?? '',
        answer: json['answer'] as String? ?? '',
      );
}

/// Planner-facing registration record returned by the backend.
class PlannerRegistrationModel {
  final int id;
  final String fullName;
  final String emailAddress;
  final String? organisation;
  final String? phoneNumber;
  final RegistrationStatus status;
  final RsvpStatus? rsvpStatus;
  final String? emailDeliveryStatus;
  final DateTime registeredAt;
  final DateTime? confirmedAt;
  final DateTime? cancelledAt;
  final int deliveryAttempts;
  final DateTime? sentAt;
  final DateTime? checkedInAt;
  final String? checkedInMethod;
  final List<RegistrationAnswerModel> answers;

  const PlannerRegistrationModel({
    required this.id,
    required this.fullName,
    required this.emailAddress,
    this.organisation,
    this.phoneNumber,
    required this.status,
    this.rsvpStatus,
    this.emailDeliveryStatus,
    required this.registeredAt,
    this.confirmedAt,
    this.cancelledAt,
    required this.deliveryAttempts,
    this.sentAt,
    this.checkedInAt,
    this.checkedInMethod,
    required this.answers,
  });

  bool get isCheckedIn => checkedInAt != null;

  factory PlannerRegistrationModel.fromJson(Map<String, dynamic> json) =>
      PlannerRegistrationModel(
        id: (json['id'] as num?)?.toInt() ?? 0,
        fullName: json['fullName'] as String? ?? '',
        emailAddress: json['emailAddress'] as String? ?? '',
        organisation: json['organisation'] as String?,
        phoneNumber: json['phoneNumber'] as String?,
        status: _parseRegistrationStatus(json['status'] as String?),
        rsvpStatus: json['rsvpStatus'] == null
            ? null
            : _parseRsvpStatus(json['rsvpStatus'] as String?),
        emailDeliveryStatus: json['emailDeliveryStatus'] as String?,
        registeredAt: DateTime.parse(json['registeredAt'] as String),
        confirmedAt: json['confirmedAt'] == null
            ? null
            : DateTime.parse(json['confirmedAt'] as String),
        cancelledAt: json['cancelledAt'] == null
            ? null
            : DateTime.parse(json['cancelledAt'] as String),
        deliveryAttempts: (json['deliveryAttempts'] as num?)?.toInt() ?? 0,
        sentAt: json['sentAt'] == null
            ? null
            : DateTime.parse(json['sentAt'] as String),
        checkedInAt: json['checkedInAt'] == null
            ? null
            : DateTime.parse(json['checkedInAt'] as String),
        checkedInMethod: json['checkedInMethod'] as String?,
        answers: (json['answers'] as List<dynamic>? ?? [])
            .map((a) =>
                RegistrationAnswerModel.fromJson(a as Map<String, dynamic>))
            .toList(),
      );
}

/// Page of planner registrations
class PlannerRegistrationPage {
  final List<PlannerRegistrationModel> items;
  final int total;
  final int page;
  final int pageSize;

  const PlannerRegistrationPage({
    required this.items,
    required this.total,
    required this.page,
    required this.pageSize,
  });

  factory PlannerRegistrationPage.fromJson(Map<String, dynamic> json) =>
      PlannerRegistrationPage(
        items: (json['items'] as List<dynamic>? ?? [])
            .map((item) => PlannerRegistrationModel.fromJson(
                item as Map<String, dynamic>))
            .toList(),
        total: (json['total'] as num?)?.toInt() ?? 0,
        page: (json['page'] as num?)?.toInt() ?? 1,
        pageSize: (json['pageSize'] as num?)?.toInt() ?? 50,
      );
}

/// Public registration response (guest-facing after submission or status check).
class PublicRegistrationModel {
  final String publicReference;
  final RegistrationStatus status;
  final String? statusSecret;
  final String? invitationToken;
  final String? qrPngBase64;
  final RsvpStatus? rsvpStatus;
  final String? emailDeliveryStatus;
  final DateTime registeredAt;
  final DateTime? confirmedAt;
  final DateTime? cancelledAt;

  const PublicRegistrationModel({
    required this.publicReference,
    required this.status,
    this.statusSecret,
    this.invitationToken,
    this.qrPngBase64,
    this.rsvpStatus,
    this.emailDeliveryStatus,
    required this.registeredAt,
    this.confirmedAt,
    this.cancelledAt,
  });

  factory PublicRegistrationModel.fromJson(Map<String, dynamic> json) =>
      PublicRegistrationModel(
        publicReference: json['publicReference'] as String? ?? '',
        status: _parseRegistrationStatus(json['status'] as String?),
        statusSecret: json['statusSecret'] as String?,
        invitationToken: json['invitationToken'] as String?,
        qrPngBase64: json['qrPngBase64'] as String?,
        rsvpStatus: json['rsvpStatus'] == null
            ? null
            : _parseRsvpStatus(json['rsvpStatus'] as String?),
        emailDeliveryStatus: json['emailDeliveryStatus'] as String?,
        registeredAt: DateTime.parse(json['registeredAt'] as String),
        confirmedAt: json['confirmedAt'] == null
            ? null
            : DateTime.parse(json['confirmedAt'] as String),
        cancelledAt: json['cancelledAt'] == null
            ? null
            : DateTime.parse(json['cancelledAt'] as String),
      );
}

/// Per-row validation error from bulk upload.
class GuestUploadRowError {
  final int rowNumber;
  final String field;
  final String message;

  const GuestUploadRowError({
    required this.rowNumber,
    required this.field,
    required this.message,
  });

  factory GuestUploadRowError.fromJson(Map<String, dynamic> json) =>
      GuestUploadRowError(
        rowNumber: (json['rowNumber'] as num?)?.toInt() ?? 0,
        field: json['field'] as String? ?? '',
        message: json['message'] as String? ?? '',
      );
}

/// Structured result from the bulk guest list upload endpoint.
class BulkUploadResult {
  final int totalRows;
  final int successfulRows;
  final int failedRows;
  final int duplicateRows;
  final int alreadyRegisteredRows;
  final List<GuestUploadRowError> errors;

  const BulkUploadResult({
    required this.totalRows,
    required this.successfulRows,
    required this.failedRows,
    required this.duplicateRows,
    required this.alreadyRegisteredRows,
    required this.errors,
  });

  factory BulkUploadResult.fromJson(Map<String, dynamic> json) =>
      BulkUploadResult(
        totalRows: (json['totalRows'] as num?)?.toInt() ?? 0,
        successfulRows: (json['successfulRows'] as num?)?.toInt() ?? 0,
        failedRows: (json['failedRows'] as num?)?.toInt() ?? 0,
        duplicateRows: (json['duplicateRows'] as num?)?.toInt() ?? 0,
        alreadyRegisteredRows:
            (json['alreadyRegisteredRows'] as num?)?.toInt() ?? 0,
        errors: (json['errors'] as List<dynamic>? ?? [])
            .map((e) =>
                GuestUploadRowError.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// Check-in result returned by the backend.
class CheckInResultModel {
  final String message;
  final PlannerRegistrationModel guest;

  const CheckInResultModel({
    required this.message,
    required this.guest,
  });

  factory CheckInResultModel.fromJson(Map<String, dynamic> json) =>
      CheckInResultModel(
        message: json['message'] as String? ?? '',
        guest: PlannerRegistrationModel.fromJson(
            json['guest'] as Map<String, dynamic>),
      );
}
