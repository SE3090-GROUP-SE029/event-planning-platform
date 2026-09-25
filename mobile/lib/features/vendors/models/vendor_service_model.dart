class VendorServiceModel {
  final String id;
  final String vendorId;
  final String serviceName;
  final String? description;
  final double? price;
  final String? pricingType;

  VendorServiceModel({
    required this.id,
    required this.vendorId,
    required this.serviceName,
    required this.description,
    this.price,
    this.pricingType,
  });

  factory VendorServiceModel.fromJson(Map<String, dynamic> json) {
    return VendorServiceModel(
      id: json['id']?.toString() ?? '',
      vendorId: json['vendorId']?.toString() ?? '',
      serviceName: json['serviceName'] as String? ?? '',
      description: json['description'] as String?,
      price: json['price'] == null ? null : (json['price'] as num).toDouble(),
      pricingType: json['pricingType'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'serviceName': serviceName,
      'description': description,
      'price': price,
      'pricingType': pricingType,
    };
  }

  String get displayPrice {
    if (price == null) {
      return 'Price not set';
    }

    final formatted = 'Rs. ${price!.toStringAsFixed(2)} LKR';
    switch (pricingType) {
      case 'PER_PERSON':
        return '$formatted · per person';
      case 'PER_HOUR':
        return '$formatted · per hour';
      case 'PER_DAY':
        return '$formatted · per day';
      case 'FIXED':
        return '$formatted · fixed';
      default:
        return formatted;
    }
  }
}
