import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/guest_management/api/guest_management_remote_datasource.dart';
import 'package:mobile/features/guest_management/models/registration_form_model.dart';
import 'package:mobile/features/guest_management/pages/registration_form_setup_page.dart';
import 'package:mobile/features/guest_management/providers/guest_management_providers.dart';

class _MissingFormDataSource extends GuestManagementRemoteDataSource {
  _MissingFormDataSource() : super(dio: Dio());

  @override
  Future<PlannerFormModel> getForm(String eventId) async {
    final request =
        RequestOptions(path: '/api/events/$eventId/registration-form');
    throw DioException(
      requestOptions: request,
      response: Response(requestOptions: request, statusCode: 404),
    );
  }
}

void main() {
  testWidgets('shows required registration settings when form is missing',
      (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          guestManagementApiProvider
              .overrideWithValue(_MissingFormDataSource()),
        ],
        child: const MaterialApp(
          home: RegistrationFormSetupPage(
            eventId: 'event-id',
            eventName: 'Test Event',
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Test Event'), findsOneWidget);
    expect(find.text('Registration opens'), findsOneWidget);
    expect(find.text('Registration closes'), findsOneWidget);
    expect(find.text('Seat limit'), findsOneWidget);
    expect(find.text('Save and publish'), findsOneWidget);
  });

  testWidgets('does not submit with an invalid seat limit', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          guestManagementApiProvider
              .overrideWithValue(_MissingFormDataSource()),
        ],
        child: const MaterialApp(
          home: RegistrationFormSetupPage(
            eventId: 'event-id',
            eventName: 'Test Event',
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Save and publish'));
    await tester.pumpAndSettle();

    expect(find.text('Enter a seat limit of at least 1.'), findsOneWidget);
  });
}
