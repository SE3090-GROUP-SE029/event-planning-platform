import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/vendors/models/marketplace_vendor_model.dart';

void main() {
  group('MarketplaceVendorListResult.fromJson', () {
    test('parses a valid empty marketplace response', () {
      final result = MarketplaceVendorListResult.fromJson({
        'items': [],
        'page': 1,
        'pageSize': 6,
        'totalCount': 0,
        'totalPages': 0,
      });

      expect(result.items, isEmpty);
      expect(result.totalCount, 0);
    });

    test('rejects a response that has no items array', () {
      expect(
        () => MarketplaceVendorListResult.fromJson({
          'page': 1,
          'pageSize': 6,
          'totalCount': 4,
          'totalPages': 1,
        }),
        throwsFormatException,
      );
    });
  });
}
