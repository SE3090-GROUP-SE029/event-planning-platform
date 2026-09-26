class QuotationModel {
  final String id;
  final String eventId;
  final String? eventType;
  final int? guestCount;
  final DateTime? eventPreferredDate;
  final String? eventRequirements;
  final String vendorId;
  final String vendorBusinessName;
  final String vendorServiceId;
  final String serviceName;
  final String requestedByUserId;
  final DateTime requestedStartDateTime;
  final DateTime requestedEndDateTime;
  final String? customerMessage;
  final double? quotedPrice;
  final String? vendorTerms;
  final String status;
  final DateTime requestedAt;
  final DateTime? respondedAt;

  const QuotationModel({
    required this.id,
    required this.eventId,
    required this.eventType,
    required this.guestCount,
    required this.eventPreferredDate,
    required this.eventRequirements,
    required this.vendorId,
    required this.vendorBusinessName,
    required this.vendorServiceId,
    required this.serviceName,
    required this.requestedByUserId,
    required this.requestedStartDateTime,
    required this.requestedEndDateTime,
    required this.customerMessage,
    required this.quotedPrice,
    required this.vendorTerms,
    required this.status,
    required this.requestedAt,
    required this.respondedAt,
  });

  factory QuotationModel.fromJson(Map<String, dynamic> json) => QuotationModel(
        id: '${json['id'] ?? ''}',
        eventId: '${json['eventId'] ?? ''}',
        eventType: json['eventType']?.toString(),
        guestCount: json['guestCount'] is num
            ? (json['guestCount'] as num).toInt()
            : int.tryParse('${json['guestCount'] ?? ''}'),
        eventPreferredDate: json['eventPreferredDate'] != null
            ? DateTime.tryParse(json['eventPreferredDate'].toString())
            : null,
        eventRequirements: json['eventRequirements']?.toString(),
        vendorId: '${json['vendorId'] ?? ''}',
        vendorBusinessName: json['vendorBusinessName']?.toString() ?? 'Vendor',
        vendorServiceId: '${json['vendorServiceId'] ?? ''}',
        serviceName: json['serviceName']?.toString() ?? 'Service',
        requestedByUserId: '${json['requestedByUserId'] ?? ''}',
        requestedStartDateTime: DateTime.parse(
          json['requestedStartDateTime'].toString(),
        ),
        requestedEndDateTime: DateTime.parse(
          json['requestedEndDateTime'].toString(),
        ),
        customerMessage: json['customerMessage']?.toString(),
        quotedPrice: json['quotedPrice'] is num
            ? (json['quotedPrice'] as num).toDouble()
            : double.tryParse('${json['quotedPrice'] ?? ''}'),
        vendorTerms: json['vendorTerms']?.toString(),
        status: json['status']?.toString() ?? 'REQUESTED',
        requestedAt: DateTime.parse(json['requestedAt'].toString()),
        respondedAt: json['respondedAt'] != null
            ? DateTime.tryParse(json['respondedAt'].toString())
            : null,
      );

  String get displayStatus {
    if (status == 'REQUESTED') return 'Pending';
    if (status == 'QUOTED') return 'Responded';
    return status;
  }

  String get displayQuotedPrice {
    if (quotedPrice == null) return '—';
    return 'Rs. ${quotedPrice!.toStringAsFixed(quotedPrice! % 1 == 0 ? 0 : 2)}';
  }

  String get displayRange {
    return '${_fmt(requestedStartDateTime)} → ${_fmt(requestedEndDateTime)}';
  }

  static String _fmt(DateTime value) {
    final local = value.toLocal();
    final y = local.year.toString().padLeft(4, '0');
    final m = local.month.toString().padLeft(2, '0');
    final d = local.day.toString().padLeft(2, '0');
    final h = local.hour.toString().padLeft(2, '0');
    final min = local.minute.toString().padLeft(2, '0');
    return '$y-$m-$d $h:$min';
  }
}
