class VendorProfileModel {
  final String id;
  final String userId;
  final String businessName;
  final String category;
  final String contactEmail;
  final String contactPhone;
  final String? description;
  final String status;

  VendorProfileModel({
    required this.id,
    required this.userId,
    required this.businessName,
    required this.category,
    required this.contactEmail,
    required this.contactPhone,
    required this.description,
    required this.status,
  });

  factory VendorProfileModel.fromJson(Map<String, dynamic> json) {
    return VendorProfileModel(
      id: json['id']?.toString() ?? '',
      userId: json['userId']?.toString() ?? '',
      businessName: json['businessName'] as String? ?? '',
      category: json['category'] as String? ?? '',
      contactEmail: json['contactEmail'] as String? ?? '',
      contactPhone: json['contactPhone'] as String? ?? '',
      description: json['description'] as String?,
      status: json['status'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'businessName': businessName,
      'category': category,
      'contactEmail': contactEmail,
      'contactPhone': contactPhone,
      'description': description,
    };
  }
}
