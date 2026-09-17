import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/vendors/models/vendor_profile_model.dart';
import 'package:mobile/features/vendors/pages/vendor_profile_page.dart';

void main() {
  test('vendor profile payload includes business fields', () {
    final profile = VendorProfileModel(
      id: '1',
      userId: 'user-1',
      businessName: 'Green Leaf Catering',
      category: 'CATERING',
      contactEmail: 'hello@greenleaf.test',
      contactPhone: '0771234567',
      description: 'Veg catering',
      status: 'PENDING',
    );

    expect(profile.toJson()['businessName'], 'Green Leaf Catering');
    expect(profile.toJson()['category'], 'CATERING');
    expect(vendorCategories, contains('PHOTOGRAPHY'));
  });
}
