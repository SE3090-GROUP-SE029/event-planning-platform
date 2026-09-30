class VendorRecommendationItem {
  final String vendorId;
  final String? vendorServiceId;
  final int rank;
  final int score;
  final String reason;
  final String businessName;
  final String category;
  final String? serviceName;
  final double? price;
  final String? pricingType;
  final double? averageRating;
  final int reviewCount;
  final bool availabilityMatch;

  const VendorRecommendationItem({
    required this.vendorId,
    required this.vendorServiceId,
    required this.rank,
    required this.score,
    required this.reason,
    required this.businessName,
    required this.category,
    required this.serviceName,
    required this.price,
    required this.pricingType,
    required this.averageRating,
    required this.reviewCount,
    required this.availabilityMatch,
  });

  factory VendorRecommendationItem.fromJson(Map<String, dynamic> json) =>
      VendorRecommendationItem(
        vendorId: '${json['vendorId'] ?? ''}',
        vendorServiceId: json['vendorServiceId']?.toString(),
        rank: (json['rank'] as num?)?.toInt() ?? 0,
        score: (json['score'] as num?)?.toInt() ?? 0,
        reason: json['reason'] as String? ?? '',
        businessName: json['businessName'] as String? ?? 'Vendor',
        category: json['category'] as String? ?? '',
        serviceName: json['serviceName'] as String?,
        price: (json['price'] as num?)?.toDouble(),
        pricingType: json['pricingType'] as String?,
        averageRating: (json['averageRating'] as num?)?.toDouble(),
        reviewCount: (json['reviewCount'] as num?)?.toInt() ?? 0,
        availabilityMatch: json['availabilityMatch'] as bool? ?? false,
      );
}

class VendorRecommendationRun {
  final String id;
  final String eventId;
  final String eventPlanDraftId;
  final int candidateCount;
  final DateTime createdAt;
  final String? sourceNote;
  final bool fromCache;
  final List<VendorRecommendationItem> items;

  const VendorRecommendationRun({
    required this.id,
    required this.eventId,
    required this.eventPlanDraftId,
    required this.candidateCount,
    required this.createdAt,
    required this.sourceNote,
    required this.fromCache,
    required this.items,
  });

  factory VendorRecommendationRun.fromJson(Map<String, dynamic> json) =>
      VendorRecommendationRun(
        id: '${json['id'] ?? ''}',
        eventId: '${json['eventId'] ?? ''}',
        eventPlanDraftId: '${json['eventPlanDraftId'] ?? ''}',
        candidateCount: (json['candidateCount'] as num?)?.toInt() ?? 0,
        createdAt:
            DateTime.tryParse('${json['createdAt']}') ?? DateTime.now().toUtc(),
        sourceNote: json['sourceNote'] as String?,
        fromCache: json['fromCache'] as bool? ?? false,
        items: (json['items'] as List<dynamic>? ?? [])
            .whereType<Map<String, dynamic>>()
            .map(VendorRecommendationItem.fromJson)
            .toList(),
      );
}
