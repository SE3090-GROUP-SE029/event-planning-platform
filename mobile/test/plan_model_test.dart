import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/plans/models/plan_model.dart';

void main() {
  test('parses a plan response and preserves budget and review details', () {
    final plan = EventPlan.fromJson({
      'id': 'plan-1',
      'eventId': 'event-1',
      'status': 'PendingPlannerReview',
      'serviceCategories': ['Catering'],
      'budgetAllocation': {'Catering': 1250},
      'proposedTimeline': {'Setup': '2 hours before event'},
      'planCompletenessScore': 80,
      'eventSnapshot': {
        'eventName': 'Launch',
        'eventDate': '2027-01-01T10:00:00Z',
        'location': 'Hall A',
        'guestCount': 50,
        'budget': 2000,
      },
    });

    expect(plan.status, PlanStatus.pendingPlannerReview);
    expect(plan.completenessScore, 80);
    expect(plan.budgetAllocation['Catering'], 1250);
    expect(plan.event.guestCount, 50);
  });
}
