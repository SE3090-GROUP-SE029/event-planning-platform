import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/guest_management/api/guest_management_remote_datasource.dart';
import 'package:mobile/features/guest_management/models/guest_registration_model.dart';
import 'package:mobile/features/guest_management/pages/registration_status_page.dart';

class _StatusDataSource extends GuestManagementRemoteDataSource {
  _StatusDataSource() : super(dio: Dio());

  String? requestedReference;
  String? requestedSecret;

  @override
  Future<PublicRegistrationModel> getRegistrationStatus(
    String publicReference,
    String secret,
  ) async {
    requestedReference = publicReference;
    requestedSecret = secret;
    return PublicRegistrationModel(
      publicReference: publicReference,
      status: RegistrationStatus.confirmed,
      invitationToken: 'invitation-token',
      rsvpStatus: RsvpStatus.notResponded,
      registeredAt: DateTime(2026),
      confirmedAt: DateTime(2026),
    );
  }
}

void main() {
  testWidgets('refreshes status and keeps secret available for RSVP',
      (tester) async {
    final api = _StatusDataSource();
    final secret = List.filled(43, 's').join();
    await tester.pumpWidget(
      MaterialApp(
        home: RegistrationStatusPage(
          registration: PublicRegistrationModel(
            publicReference: 'reference',
            status: RegistrationStatus.pendingReview,
            statusSecret: secret,
            registeredAt: DateTime(2026),
          ),
          eventName: 'Test Event',
          api: api,
        ),
      ),
    );

    expect(find.text('UNDER REVIEW'), findsOneWidget);
    await tester.tap(find.text('Refresh status'));
    await tester.pumpAndSettle();

    expect(api.requestedReference, 'reference');
    expect(api.requestedSecret, secret);
    expect(find.text("You're In!"), findsOneWidget);
    expect(find.text('Submit RSVP'), findsOneWidget);

    await tester.tap(find.text('Submit RSVP'));
    await tester.pumpAndSettle();
    expect(find.text('RSVP'), findsOneWidget);
    expect(
      find.text('Your registration identity has been verified automatically.'),
      findsOneWidget,
    );
  });
}
