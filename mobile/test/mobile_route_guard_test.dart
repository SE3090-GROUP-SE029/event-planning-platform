import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/api/dio_client.dart';
import 'package:mobile/features/auth/models/auth_response_model.dart';
import 'package:mobile/main.dart'
    show isRouteAllowedForSession, isRouteArgumentsValid;

void main() {
  test('API base URL validation rejects missing and malformed values', () {
    expect(() => DioClient.validateBaseUrl(null), throwsFormatException);
    expect(() => DioClient.validateBaseUrl(''), throwsFormatException);
    expect(() => DioClient.validateBaseUrl('localhost:5207'),
        throwsFormatException);
    expect(
      DioClient.validateBaseUrl('http://10.0.2.2:5207'),
      'http://10.0.2.2:5207',
    );
  });

  test('planner and vendor route access is role-specific', () {
    final planner = _session('EVENT_PLANNER');
    final vendor = _session('VENDOR');
    final admin = _session('ADMIN');

    expect(isRouteAllowedForSession('/plans/review', planner), isTrue);
    expect(isRouteAllowedForSession('/recommendations', planner), isTrue);
    expect(isRouteAllowedForSession('/recommendations', vendor), isFalse);
    expect(isRouteAllowedForSession('/dashboard', planner), isTrue);
    expect(isRouteAllowedForSession('/vendors/quotations', planner), isFalse);
    expect(isRouteAllowedForSession('/dashboard', vendor), isTrue);
    expect(isRouteAllowedForSession('/vendors/quotations', vendor), isTrue);
    expect(isRouteAllowedForSession('/dashboard', admin), isFalse);
    expect(isRouteAllowedForSession('/events', null), isFalse);
  });

  test('route argument validation prevents missing plan IDs', () {
    expect(isRouteArgumentsValid('/plans/review', 'plan-1'), isTrue);
    expect(isRouteArgumentsValid('/plans/review', null), isFalse);
    expect(isRouteArgumentsValid('/plans/review', '   '), isFalse);
    expect(
        isRouteArgumentsValid('/recommendations', {'eventId': 'event-1'}),
        isTrue);
    expect(isRouteArgumentsValid('/recommendations', {}), isFalse);
    expect(isRouteArgumentsValid('/marketplace/details', {}), isFalse);
  });
}

AuthResponseModel _session(String role) => AuthResponseModel(
      accessToken: 'access',
      refreshToken: 'refresh',
      accessTokenExpiresAt: '2099-01-01T00:00:00Z',
      userId: 'user-1',
      email: 'user@test.com',
      roles: [role],
    );
