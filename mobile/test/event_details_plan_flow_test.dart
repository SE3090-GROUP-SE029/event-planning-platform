import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/auth/models/auth_response_model.dart';
import 'package:mobile/features/events/api/event_remote_datasource.dart';
import 'package:mobile/features/events/models/event_model.dart';
import 'package:mobile/features/events/pages/event_details_page.dart';
import 'package:mobile/features/plans/api/plan_remote_datasource.dart';
import 'package:mobile/features/plans/models/plan_model.dart';

void main() {
  testWidgets('generates a plan and navigates to its review page',
      (tester) async {
    final event = _event();
    final plan = _plan();
    final planApi = _TestPlanRemoteDataSource(
      generatedPlan: plan,
    );

    await tester.pumpWidget(_app(
      event,
      _TestEventRemoteDataSource(event),
      planApi,
    ));
    await tester.tap(find.text('Open event'));
    await tester.pumpAndSettle();
    expect(find.text('Event details'), findsOneWidget);
    expect(find.text('Event not found.'), findsNothing);
    await tester.scrollUntilVisible(find.text('Generate AI Plan'), 300);
    await tester.tap(find.text('Generate AI Plan'));
    await tester.pumpAndSettle();

    expect(planApi.generateCallCount, 1);
    expect(find.text('Plan review: ${plan.id}'), findsOneWidget);
  });

  testWidgets('opens the latest existing plan without generating another',
      (tester) async {
    final event = _event();
    final existingPlan = _plan();
    final planApi = _TestPlanRemoteDataSource(
      existingPlans: [existingPlan],
    );

    await tester.pumpWidget(_app(
      event,
      _TestEventRemoteDataSource(event),
      planApi,
    ));
    await tester.tap(find.text('Open event'));
    await tester.pumpAndSettle();
    expect(find.text('Event details'), findsOneWidget);
    expect(find.text('Event not found.'), findsNothing);
    await tester.scrollUntilVisible(find.text('View AI Plan'), 300);
    await tester.tap(find.text('View AI Plan'));
    await tester.pumpAndSettle();

    expect(planApi.generateCallCount, 0);
    expect(find.text('Plan review: ${existingPlan.id}'), findsOneWidget);
  });

  testWidgets('shows a retry message when plan generation fails',
      (tester) async {
    final event = _event();
    final planApi = _TestPlanRemoteDataSource(
      generateError: Exception('AI service unavailable'),
    );

    await tester.pumpWidget(_app(
      event,
      _TestEventRemoteDataSource(event),
      planApi,
    ));
    await tester.tap(find.text('Open event'));
    await tester.pumpAndSettle();
    expect(find.text('Event details'), findsOneWidget);
    expect(find.text('Event not found.'), findsNothing);
    await tester.scrollUntilVisible(find.text('Generate AI Plan'), 300);
    await tester.tap(find.text('Generate AI Plan'));
    await tester.pumpAndSettle();

    expect(find.text('We could not generate the AI plan. Please try again.'),
        findsOneWidget);
    expect(find.text('Retry'), findsOneWidget);
    expect(find.textContaining('Plan review:'), findsNothing);
  });

  testWidgets('shows the server error when plan generation is rejected',
      (tester) async {
    final event = _event();
    const serverMessage =
        'The AI planning service is temporarily unavailable. Please try again.';
    final request = RequestOptions(path: '/api/events/event-1/plans/generate');
    final planApi = _TestPlanRemoteDataSource(
      generateError: DioException(
        requestOptions: request,
        response: Response<dynamic>(
          requestOptions: request,
          statusCode: 503,
          data: {'success': false, 'message': serverMessage},
        ),
      ),
    );

    await tester.pumpWidget(_app(
      event,
      _TestEventRemoteDataSource(event),
      planApi,
    ));
    await tester.tap(find.text('Open event'));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(find.text('Generate AI Plan'), 300);
    await tester.tap(find.text('Generate AI Plan'));
    await tester.pumpAndSettle();

    expect(find.text(serverMessage), findsOneWidget);
  });

  testWidgets('hides plan actions from a non-owner', (tester) async {
    final event = _event();
    await tester.pumpWidget(_app(
      event,
      _TestEventRemoteDataSource(event),
      _TestPlanRemoteDataSource(),
      auth: _auth(userId: 'another-owner'),
    ));
    await tester.tap(find.text('Open event'));
    await tester.pumpAndSettle();

    expect(find.text('AI event plan'), findsNothing);
    expect(find.text('Generate AI Plan'), findsNothing);
  });
}

Widget _app(EventModel event, EventRemoteDataSource eventApi,
    PlanRemoteDataSource planApi,
    {AuthResponseModel? auth}) {
  return MaterialApp(
    home: Builder(
      builder: (context) => Scaffold(
        body: Center(
          child: TextButton(
            onPressed: () => Navigator.pushNamed(
              context,
              '/events/details',
              arguments: {'auth': auth ?? _auth(), 'event': event},
            ),
            child: const Text('Open event'),
          ),
        ),
      ),
    ),
    onGenerateRoute: (settings) {
      if (settings.name == '/events/details') {
        return MaterialPageRoute<void>(
          settings: settings,
          builder: (_) => EventDetailsPage(
            eventApi: eventApi,
            planApi: planApi,
          ),
        );
      }
      if (settings.name == '/plans/review') {
        return MaterialPageRoute<void>(
          settings: settings,
          builder: (_) => Scaffold(
            body: Center(child: Text('Plan review: ${settings.arguments}')),
          ),
        );
      }
      return null;
    },
  );
}

AuthResponseModel _auth({
  String userId = 'owner-1',
  List<String> roles = const [],
}) =>
    AuthResponseModel(
      accessToken: 'test-token',
      refreshToken: 'test-refresh-token',
      accessTokenExpiresAt: '',
      userId: userId,
      email: 'owner@example.test',
      roles: roles,
    );

EventModel _event() => EventModel(
      id: 'event-1',
      ownerId: 'owner-1',
      eventType: EventType.other,
      guestCount: 50,
      budget: 1000,
      preferredVenue: 'Community Hall',
      preferredDate: DateTime.utc(2027, 1, 1),
      eventDuration: const Duration(hours: 1),
      requirements: null,
      status: EventStatus.draft,
      createdAt: DateTime.utc(2026, 1, 1),
      updatedAt: null,
    );

EventPlan _plan() => EventPlan(
      id: 'plan-1',
      eventId: 'event-1',
      version: 1,
      status: PlanStatus.pendingPlannerReview,
      serviceCategories: const ['Venue'],
      budgetAllocation: const {'Venue': 1000},
      proposedTimeline: const {},
      rationale: 'Plan rationale',
      completenessScore: 80,
      validationSummary: '',
      risks: const [],
      missingRequirements: const [],
      event: EventSnapshot(
        eventName: 'Community Hall',
        eventDate: DateTime.utc(2027, 1, 1),
        location: 'Community Hall',
        guestCount: 50,
        budget: 1000,
      ),
      plannerRemarks: null,
    );

class _TestEventRemoteDataSource extends EventRemoteDataSource {
  final EventModel event;

  _TestEventRemoteDataSource(this.event) : super(dio: Dio());

  @override
  Future<EventModel> get(String token, String id) async => event;
}

class _TestPlanRemoteDataSource extends PlanRemoteDataSource {
  final List<EventPlan> existingPlans;
  final EventPlan? generatedPlan;
  final Object? generateError;
  int generateCallCount = 0;

  _TestPlanRemoteDataSource({
    this.existingPlans = const [],
    this.generatedPlan,
    this.generateError,
  }) : super(dio: Dio());

  @override
  Future<List<EventPlan>> listForEvent(String token, String eventId) async =>
      existingPlans;

  @override
  Future<EventPlan> generate(String token, String eventId) async {
    generateCallCount++;
    if (generateError != null) throw generateError!;
    return generatedPlan!;
  }
}
