class MarketplaceVendorSummary {
  final String id;
  final String businessName;
  final String category;
  final String? shortDescription;
  final String address;
  final String? profileImageUrl;
  final double? startingPrice;
  final String? startingPricingType;

  MarketplaceVendorSummary({
    required this.id,
    required this.businessName,
    required this.category,
    required this.shortDescription,
    required this.address,
    required this.profileImageUrl,
    required this.startingPrice,
    required this.startingPricingType,
  });

  factory MarketplaceVendorSummary.fromJson(Map<String, dynamic> json) {
    return MarketplaceVendorSummary(
      id: json['id']?.toString() ?? '',
      businessName: json['businessName'] as String? ?? '',
      category: json['category'] as String? ?? '',
      shortDescription: json['shortDescription'] as String?,
      address: json['address'] as String? ?? '',
      profileImageUrl: json['profileImageUrl'] as String?,
      startingPrice:
          json['startingPrice'] == null ? null : (json['startingPrice'] as num).toDouble(),
      startingPricingType: json['startingPricingType'] as String?,
    );
  }

  String get displayStartingPrice {
    if (startingPrice == null) return 'Price on request';
    final amount = 'Rs. ${startingPrice!.toStringAsFixed(0)}';
    switch (startingPricingType) {
      case 'FIXED':
        return '$amount — Fixed';
      case 'PER_PERSON':
        return '$amount — Per person';
      case 'PER_HOUR':
        return '$amount — Per hour';
      case 'PER_DAY':
        return '$amount — Per day';
      default:
        return amount;
    }
  }
}

class MarketplaceVendorListResult {
  final List<MarketplaceVendorSummary> items;
  final int page;
  final int pageSize;
  final int totalCount;
  final int totalPages;

  MarketplaceVendorListResult({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.totalPages,
  });

  factory MarketplaceVendorListResult.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'] as List<dynamic>? ?? [];
    return MarketplaceVendorListResult(
      items: rawItems
          .map((item) =>
              MarketplaceVendorSummary.fromJson(item as Map<String, dynamic>))
          .toList(),
      page: json['page'] as int? ?? 1,
      pageSize: json['pageSize'] as int? ?? 10,
      totalCount: json['totalCount'] as int? ?? 0,
      totalPages: json['totalPages'] as int? ?? 0,
    );
  }
}

class MarketplaceServiceItem {
  final String id;
  final String serviceName;
  final String? description;
  final double? price;
  final String? pricingType;

  MarketplaceServiceItem({
    required this.id,
    required this.serviceName,
    required this.description,
    required this.price,
    required this.pricingType,
  });

  factory MarketplaceServiceItem.fromJson(Map<String, dynamic> json) {
    return MarketplaceServiceItem(
      id: json['id']?.toString() ?? '',
      serviceName: json['serviceName'] as String? ?? '',
      description: json['description'] as String?,
      price: json['price'] == null ? null : (json['price'] as num).toDouble(),
      pricingType: json['pricingType'] as String?,
    );
  }

  String get displayPrice {
    if (price == null) return 'Price on request';
    final amount = 'Rs. ${price!.toStringAsFixed(0)}';
    switch (pricingType) {
      case 'FIXED':
        return '$amount — Fixed';
      case 'PER_PERSON':
        return '$amount — Per person';
      case 'PER_HOUR':
        return '$amount — Per hour';
      case 'PER_DAY':
        return '$amount — Per day';
      default:
        return amount;
    }
  }
}

class MarketplaceGalleryItem {
  final String id;
  final String imageUrl;

  MarketplaceGalleryItem({required this.id, required this.imageUrl});

  factory MarketplaceGalleryItem.fromJson(Map<String, dynamic> json) {
    return MarketplaceGalleryItem(
      id: json['id']?.toString() ?? '',
      imageUrl: json['imageUrl'] as String? ?? '',
    );
  }
}

class MarketplaceAvailabilityItem {
  final String id;
  final DateTime startDateTime;
  final DateTime endDateTime;
  final bool isAvailable;

  MarketplaceAvailabilityItem({
    required this.id,
    required this.startDateTime,
    required this.endDateTime,
    required this.isAvailable,
  });

  factory MarketplaceAvailabilityItem.fromJson(Map<String, dynamic> json) {
    return MarketplaceAvailabilityItem(
      id: json['id']?.toString() ?? '',
      startDateTime: DateTime.parse(json['startDateTime'] as String).toUtc(),
      endDateTime: DateTime.parse(json['endDateTime'] as String).toUtc(),
      isAvailable: json['isAvailable'] as bool? ?? false,
    );
  }

  String get displayRange {
    String fmt(DateTime value) {
      final local = value.toLocal();
      final y = local.year.toString().padLeft(4, '0');
      final m = local.month.toString().padLeft(2, '0');
      final d = local.day.toString().padLeft(2, '0');
      final h = local.hour.toString().padLeft(2, '0');
      final min = local.minute.toString().padLeft(2, '0');
      return '$y-$m-$d $h:$min';
    }

    return '${fmt(startDateTime)} → ${fmt(endDateTime)}';
  }
}

class MarketplaceVendorDetail {
  final String id;
  final String businessName;
  final String category;
  final String? description;
  final String address;
  final String? profileImageUrl;
  final String? websiteUrl;
  final List<MarketplaceGalleryItem> images;
  final List<MarketplaceServiceItem> services;
  final List<MarketplaceAvailabilityItem> availability;

  MarketplaceVendorDetail({
    required this.id,
    required this.businessName,
    required this.category,
    required this.description,
    required this.address,
    required this.profileImageUrl,
    required this.websiteUrl,
    required this.images,
    required this.services,
    required this.availability,
  });

  factory MarketplaceVendorDetail.fromJson(Map<String, dynamic> json) {
    return MarketplaceVendorDetail(
      id: json['id']?.toString() ?? '',
      businessName: json['businessName'] as String? ?? '',
      category: json['category'] as String? ?? '',
      description: json['description'] as String?,
      address: json['address'] as String? ?? '',
      profileImageUrl: json['profileImageUrl'] as String?,
      websiteUrl: json['websiteUrl'] as String?,
      images: (json['images'] as List<dynamic>? ?? [])
          .map((item) =>
              MarketplaceGalleryItem.fromJson(item as Map<String, dynamic>))
          .toList(),
      services: (json['services'] as List<dynamic>? ?? [])
          .map((item) =>
              MarketplaceServiceItem.fromJson(item as Map<String, dynamic>))
          .toList(),
      availability: (json['availability'] as List<dynamic>? ?? [])
          .map((item) => MarketplaceAvailabilityItem.fromJson(
              item as Map<String, dynamic>))
          .toList(),
    );
  }
}
