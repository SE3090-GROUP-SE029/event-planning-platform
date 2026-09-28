class VendorAnalyticsRecentBooking {
  final String id;
  final String serviceName;
  final String status;
  final DateTime startDateTime;
  final DateTime endDateTime;
  final double agreedPrice;
  final DateTime createdAt;
  final bool hasReview;

  const VendorAnalyticsRecentBooking({
    required this.id,
    required this.serviceName,
    required this.status,
    required this.startDateTime,
    required this.endDateTime,
    required this.agreedPrice,
    required this.createdAt,
    required this.hasReview,
  });

  factory VendorAnalyticsRecentBooking.fromJson(Map<String, dynamic> json) {
    return VendorAnalyticsRecentBooking(
      id: '${json['id'] ?? ''}',
      serviceName: json['serviceName']?.toString() ?? 'Service',
      status: json['status']?.toString() ?? '',
      startDateTime: DateTime.parse(json['startDateTime'].toString()),
      endDateTime: DateTime.parse(json['endDateTime'].toString()),
      agreedPrice: json['agreedPrice'] is num
          ? (json['agreedPrice'] as num).toDouble()
          : double.tryParse('${json['agreedPrice'] ?? ''}') ?? 0,
      createdAt: DateTime.parse(json['createdAt'].toString()),
      hasReview: json['hasReview'] == true,
    );
  }

  String get displayPrice {
    final fixed = agreedPrice % 1 == 0 ? 0 : 2;
    return 'Rs. ${agreedPrice.toStringAsFixed(fixed)}';
  }

  String get displayRange {
    String fmt(DateTime value) {
      final local = value.toLocal();
      final y = local.year.toString().padLeft(4, '0');
      final m = local.month.toString().padLeft(2, '0');
      final d = local.day.toString().padLeft(2, '0');
      return '$y-$m-$d';
    }

    return '${fmt(startDateTime)} → ${fmt(endDateTime)}';
  }
}

class VendorAnalyticsModel {
  final int totalBookings;
  final int confirmedBookings;
  final int completedBookings;
  final int cancelledBookings;
  final double totalRevenue;
  final double? averageRating;
  final int reviewCount;
  final int totalQuotations;
  final int quotationsAccepted;
  final List<VendorAnalyticsRecentBooking> recentBookings;

  const VendorAnalyticsModel({
    required this.totalBookings,
    required this.confirmedBookings,
    required this.completedBookings,
    required this.cancelledBookings,
    required this.totalRevenue,
    required this.averageRating,
    required this.reviewCount,
    required this.totalQuotations,
    required this.quotationsAccepted,
    required this.recentBookings,
  });

  factory VendorAnalyticsModel.fromJson(Map<String, dynamic> json) {
    final recent = (json['recentBookings'] as List<dynamic>? ?? [])
        .map((item) => VendorAnalyticsRecentBooking.fromJson(
              item as Map<String, dynamic>,
            ))
        .toList();

    return VendorAnalyticsModel(
      totalBookings: _asInt(json['totalBookings']),
      confirmedBookings: _asInt(json['confirmedBookings']),
      completedBookings: _asInt(json['completedBookings']),
      cancelledBookings: _asInt(json['cancelledBookings']),
      totalRevenue: json['totalRevenue'] is num
          ? (json['totalRevenue'] as num).toDouble()
          : double.tryParse('${json['totalRevenue'] ?? ''}') ?? 0,
      averageRating: json['averageRating'] == null
          ? null
          : (json['averageRating'] as num).toDouble(),
      reviewCount: _asInt(json['reviewCount']),
      totalQuotations: _asInt(json['totalQuotations']),
      quotationsAccepted: _asInt(json['quotationsAccepted']),
      recentBookings: recent,
    );
  }

  String get displayRevenue {
    final fixed = totalRevenue % 1 == 0 ? 0 : 2;
    return 'Rs. ${totalRevenue.toStringAsFixed(fixed)}';
  }

  String get displayRating {
    if (reviewCount <= 0 || averageRating == null) {
      return 'No reviews yet';
    }
    return '★ ${averageRating!.toStringAsFixed(1)} ($reviewCount)';
  }

  static int _asInt(dynamic value) {
    if (value is num) return value.toInt();
    return int.tryParse('$value') ?? 0;
  }
}
