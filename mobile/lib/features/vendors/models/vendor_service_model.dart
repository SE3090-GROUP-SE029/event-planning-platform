class VendorServiceModel {
  final String id;
  final String vendorId;
  final String serviceName;
  final String? description;

  VendorServiceModel({
    required this.id,
    required this.vendorId,
    required this.serviceName,
    required this.description,
  });

  factory VendorServiceModel.fromJson(Map<String, dynamic> json) {
    return VendorServiceModel(
      id: json['id']?.toString() ?? '',
      vendorId: json['vendorId']?.toString() ?? '',
      serviceName: json['serviceName'] as String? ?? '',
      description: json['description'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'serviceName': serviceName,
      'description': description,
    };
  }
}
