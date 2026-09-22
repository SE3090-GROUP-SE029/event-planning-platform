class VendorGalleryImageModel {
  final String id;
  final String vendorId;
  final String imageUrl;

  VendorGalleryImageModel({
    required this.id,
    required this.vendorId,
    required this.imageUrl,
  });

  factory VendorGalleryImageModel.fromJson(Map<String, dynamic> json) {
    return VendorGalleryImageModel(
      id: json['id']?.toString() ?? '',
      vendorId: json['vendorId']?.toString() ?? '',
      imageUrl: json['imageUrl'] as String? ?? '',
    );
  }
}
