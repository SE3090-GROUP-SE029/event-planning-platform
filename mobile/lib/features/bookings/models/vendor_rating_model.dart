class VendorRatingModel {
  final String id;
  final String bookingId;
  final String vendorId;
  final String reviewerUserId;
  final int rating;
  final String? comment;
  final DateTime createdAt;

  const VendorRatingModel({
    required this.id,
    required this.bookingId,
    required this.vendorId,
    required this.reviewerUserId,
    required this.rating,
    required this.comment,
    required this.createdAt,
  });

  factory VendorRatingModel.fromJson(Map<String, dynamic> json) =>
      VendorRatingModel(
        id: '${json['id'] ?? ''}',
        bookingId: '${json['bookingId'] ?? ''}',
        vendorId: '${json['vendorId'] ?? ''}',
        reviewerUserId: '${json['reviewerUserId'] ?? ''}',
        rating: json['rating'] is num
            ? (json['rating'] as num).toInt()
            : int.tryParse('${json['rating'] ?? ''}') ?? 0,
        comment: json['comment']?.toString(),
        createdAt: DateTime.parse(json['createdAt'].toString()),
      );

  String get displayStars => '★ $rating / 5';
}
