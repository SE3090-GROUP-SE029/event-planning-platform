class VendorAvailabilityModel {
  final String id;
  final String vendorId;
  final DateTime startDateTime;
  final DateTime endDateTime;
  final bool isAvailable;

  VendorAvailabilityModel({
    required this.id,
    required this.vendorId,
    required this.startDateTime,
    required this.endDateTime,
    required this.isAvailable,
  });

  factory VendorAvailabilityModel.fromJson(Map<String, dynamic> json) {
    return VendorAvailabilityModel(
      id: json['id']?.toString() ?? '',
      vendorId: json['vendorId']?.toString() ?? '',
      startDateTime: DateTime.parse(json['startDateTime'] as String).toUtc(),
      endDateTime: DateTime.parse(json['endDateTime'] as String).toUtc(),
      isAvailable: json['isAvailable'] as bool? ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'startDateTime': startDateTime.toUtc().toIso8601String(),
      'endDateTime': endDateTime.toUtc().toIso8601String(),
      'isAvailable': isAvailable,
    };
  }

  String get displayRange {
    final start = startDateTime.toLocal();
    final end = endDateTime.toLocal();
    String fmt(DateTime value) {
      final y = value.year.toString().padLeft(4, '0');
      final m = value.month.toString().padLeft(2, '0');
      final d = value.day.toString().padLeft(2, '0');
      final h = value.hour.toString().padLeft(2, '0');
      final min = value.minute.toString().padLeft(2, '0');
      return '$y-$m-$d $h:$min';
    }

    return '${fmt(start)} → ${fmt(end)}';
  }
}
