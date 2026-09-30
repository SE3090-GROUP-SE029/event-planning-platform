import 'package:flutter_test/flutter_test.dart';

import 'package:mobile/features/recommendations/models/vendor_recommendation_model.dart';

void main() {
  test('VendorRecommendationRun parses API payload', () {
    final run = VendorRecommendationRun.fromJson({
      'id': '11111111-1111-1111-1111-111111111111',
      'eventId': '22222222-2222-2222-2222-222222222222',
      'eventPlanDraftId': '33333333-3333-3333-3333-333333333333',
      'candidateCount': 4,
      'createdAt': '2026-09-28T12:00:00Z',
      'sourceNote': null,
      'fromCache': false,
      'items': [
        {
          'vendorId': '44444444-4444-4444-4444-444444444444',
          'vendorServiceId': '55555555-5555-5555-5555-555555555555',
          'rank': 1,
          'score': 91,
          'reason': 'Fits catering budget and guest count.',
          'businessName': 'Taste Co',
          'category': 'CATERING',
          'serviceName': 'Buffet',
          'price': 40,
          'pricingType': 'PER_PERSON',
          'averageRating': 4.5,
          'reviewCount': 8,
          'availabilityMatch': true,
        }
      ],
    });

    expect(run.items, hasLength(1));
    expect(run.items.first.businessName, 'Taste Co');
    expect(run.items.first.score, 91);
    expect(run.items.first.averageRating, 4.5);
    expect(run.items.first.availabilityMatch, isTrue);
  });
}
