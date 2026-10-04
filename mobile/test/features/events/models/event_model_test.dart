import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/events/models/event_model.dart';

void main() {
  test('event write payload serializes the preferred date as UTC ISO-8601', () {
    final event = EventModel(
      id: '',
      ownerId: 'owner',
      eventName: 'UTC test',
      eventType: EventType.other,
      guestCount: 10,
      budget: 100,
      preferredVenue: 'Hall',
      preferredDate: DateTime(2027, 1, 5, 10),
      eventDuration: const Duration(hours: 1),
      requirements: null,
      status: EventStatus.draft,
      createdAt: DateTime.utc(2026, 1, 1),
      updatedAt: null,
    );

    final jsonDate = event.toCreateJson()['preferredDate'] as String;

    expect(jsonDate, endsWith('Z'));
    expect(DateTime.parse(jsonDate).isUtc, isTrue);
  });
}
