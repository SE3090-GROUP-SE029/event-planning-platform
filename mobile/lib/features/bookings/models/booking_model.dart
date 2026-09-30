class BookingModel {
  final String id;
  final String quotationId;
  final String eventId;
  final String? eventType;
  final int? guestCount;
  final DateTime? eventPreferredDate;
  final String vendorId;
  final String vendorBusinessName;
  final String vendorServiceId;
  final String serviceName;
  final String requestedByUserId;
  final DateTime startDateTime;
  final DateTime endDateTime;
  final double agreedPrice;
  final String? vendorTerms;
  final String status;
  final String? cancellationReason;
  final DateTime createdAt;
  final DateTime? completedAt;
  final DateTime? cancelledAt;
  final bool hasReview;

  const BookingModel({
    required this.id,
    required this.quotationId,
    required this.eventId,
    required this.eventType,
    required this.guestCount,
    required this.eventPreferredDate,
    required this.vendorId,
    required this.vendorBusinessName,
    required this.vendorServiceId,
    required this.serviceName,
    required this.requestedByUserId,
    required this.startDateTime,
    required this.endDateTime,
    required this.agreedPrice,
    required this.vendorTerms,
    required this.status,
    required this.cancellationReason,
    required this.createdAt,
    required this.completedAt,
    required this.cancelledAt,
    required this.hasReview,
  });

  factory BookingModel.fromJson(Map<String, dynamic> json) => BookingModel(
        id: '${json['id'] ?? ''}',
        quotationId: '${json['quotationId'] ?? ''}',
        eventId: '${json['eventId'] ?? ''}',
        eventType: json['eventType']?.toString(),
        guestCount: json['guestCount'] is num
            ? (json['guestCount'] as num).toInt()
            : int.tryParse('${json['guestCount'] ?? ''}'),
        eventPreferredDate: json['eventPreferredDate'] != null
            ? DateTime.tryParse(json['eventPreferredDate'].toString())
            : null,
        vendorId: '${json['vendorId'] ?? ''}',
        vendorBusinessName: json['vendorBusinessName']?.toString() ?? 'Vendor',
        vendorServiceId: '${json['vendorServiceId'] ?? ''}',
        serviceName: json['serviceName']?.toString() ?? 'Service',
        requestedByUserId: '${json['requestedByUserId'] ?? ''}',
        startDateTime: DateTime.parse(json['startDateTime'].toString()),
        endDateTime: DateTime.parse(json['endDateTime'].toString()),
        agreedPrice: json['agreedPrice'] is num
            ? (json['agreedPrice'] as num).toDouble()
            : double.tryParse('${json['agreedPrice'] ?? ''}') ?? 0,
        vendorTerms: json['vendorTerms']?.toString(),
        status: json['status']?.toString() ?? 'CONFIRMED',
        cancellationReason: json['cancellationReason']?.toString(),
        createdAt: DateTime.parse(json['createdAt'].toString()),
        completedAt: json['completedAt'] != null
            ? DateTime.tryParse(json['completedAt'].toString())
            : null,
        cancelledAt: json['cancelledAt'] != null
            ? DateTime.tryParse(json['cancelledAt'].toString())
            : null,
        hasReview: json['hasReview'] == true,
      );

  String get displayAgreedPrice {
    return 'Rs. ${agreedPrice.toStringAsFixed(agreedPrice % 1 == 0 ? 0 : 2)}';
  }

  String get displayRange {
    return '${_fmt(startDateTime)} → ${_fmt(endDateTime)}';
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
