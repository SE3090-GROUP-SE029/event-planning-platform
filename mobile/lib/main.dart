import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/api/dio_client.dart';
import 'core/theme/app_colors.dart';
import 'core/theme/app_dimens.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/api/auth_repository.dart';
import 'features/auth/models/auth_response_model.dart';
import 'features/auth/pages/login_page.dart';
import 'features/auth/pages/register_page.dart';
import 'features/auth/providers/auth_providers.dart';
import 'features/bookings/pages/booking_detail_page.dart';
import 'features/bookings/pages/my_bookings_page.dart';
import 'features/bookings/pages/vendor_bookings_page.dart';
import 'features/dashboard/pages/dashboard_page.dart';
import 'features/events/models/event_model.dart';
import 'features/events/pages/event_details_page.dart';
import 'features/events/pages/event_form_page.dart';
import 'features/events/pages/event_list_page.dart';
import 'features/onboarding/pages/onboarding_page.dart';
import 'features/onboarding/providers/onboarding_provider.dart';
import 'features/plans/pages/plan_review_page.dart';
import 'features/quotations/pages/my_quotations_page.dart';
import 'features/quotations/pages/request_quotation_page.dart';
import 'features/quotations/pages/vendor_quotation_detail_page.dart';
import 'features/quotations/pages/vendor_quotations_page.dart';
import 'features/vendors/pages/vendor_availability_page.dart';
import 'features/vendors/pages/vendor_marketplace_detail_page.dart';
import 'features/vendors/pages/vendor_marketplace_page.dart';
import 'features/vendors/pages/vendor_profile_page.dart';
import 'features/vendors/pages/vendor_services_page.dart';

final _rootScaffoldMessengerKey = GlobalKey<ScaffoldMessengerState>();

const _plannerRoutes = {
  '/events',
  '/events/create',
  '/events/details',
  '/events/edit',
  '/marketplace',
  '/marketplace/details',
  '/marketplace/request-quotation',
  '/quotations/mine',
  '/bookings/mine',
  '/bookings/details',
  '/plans/review',
};

const _vendorRoutes = {
  '/vendors/profile',
  '/vendors/services',
  '/vendors/availability',
  '/vendors/quotations',
  '/vendors/quotations/details',
  '/vendors/bookings',
};

const _authenticatedRoutes = {
  '/dashboard',
  ..._plannerRoutes,
  ..._vendorRoutes,
};

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const ProviderScope(child: MyApp()));
}

final startupConfigurationProvider = FutureProvider<String?>((ref) async {
  String? startupError;
  try {
    await dotenv.load(fileName: '.env.local');
  } catch (_) {
    try {
      await dotenv.load(fileName: '.env');
    } catch (_) {
      // Configuration validation below reports a useful message.
    }
  }

  try {
    DioClient.validateBaseUrl(dotenv.env['API_BASE_URL']);
  } on FormatException catch (error) {
    startupError = error.message;
  }
  return startupError;
});

class MyApp extends ConsumerWidget {
  final String? startupError;

  const MyApp({super.key, this.startupError});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (startupError != null) {
      return MaterialApp(
        title: 'Plan It',
        debugShowCheckedModeBanner: false,
        theme: AppTheme.pastelTheme,
        home: _StartupProblem(message: startupError!),
      );
    }

    final configurationState = ref.watch(startupConfigurationProvider);
    if (configurationState.isLoading) {
      return MaterialApp(
        title: 'Plan It',
        debugShowCheckedModeBanner: false,
        theme: AppTheme.pastelTheme,
        home: const _StartupScreen(),
      );
    }
    if (configurationState.hasError) {
      return MaterialApp(
        title: 'Plan It',
        debugShowCheckedModeBanner: false,
        theme: AppTheme.pastelTheme,
        home: _StartupProblem(message: configurationState.error.toString()),
      );
    }
    if (configurationState.value != null) {
      return MaterialApp(
        title: 'Plan It',
        debugShowCheckedModeBanner: false,
        theme: AppTheme.pastelTheme,
        home: _StartupProblem(message: configurationState.value!),
      );
    }

    ref.listen(authNotifierProvider, (previous, next) {
      final error = next.error;
      if (error is LogoutFailureException) {
        WidgetsBinding.instance.addPostFrameCallback((_) {
          _rootScaffoldMessengerKey.currentState
            ?..hideCurrentSnackBar()
            ..showSnackBar(
              SnackBar(content: Text(error.toString())),
            );
        });
      }
    });

    final authState = ref.watch(authNotifierProvider);
    final onboardingState = ref.watch(onboardingCompletionProvider);
    final session = authState.value;
    final Widget home;
    if (authState.isLoading || onboardingState.isLoading) {
      home = const _StartupScreen();
    } else if (onboardingState.hasError) {
      home = _StartupProblem(message: onboardingState.error.toString());
    } else if (authState.hasError &&
        authState.error is! LogoutFailureException) {
      home = _StartupProblem(message: authState.error.toString());
    } else if (session != null) {
      home = const DashboardPage();
    } else if (authState.error is LogoutFailureException ||
        onboardingState.value == false) {
      home = const OnboardingPage();
    } else {
      home = const LoginPage();
    }

    return MaterialApp(
      title: 'Plan It',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.pastelTheme,
      scaffoldMessengerKey: _rootScaffoldMessengerKey,
      home: home,
      onGenerateRoute: (settings) =>
          _generateRoute(settings, ref.read(authNotifierProvider).value),
    );
  }

  Route<dynamic> _generateRoute(
    RouteSettings settings,
    AuthResponseModel? session,
  ) {
    final name = settings.name ?? '';
    if (name == '/login' || name == '/register') {
      return MaterialPageRoute<void>(
        settings: settings,
        builder: (_) =>
            name == '/login' ? const LoginPage() : const RegisterPage(),
      );
    }

    if (name == '/onboarding' && session != null) {
      return MaterialPageRoute<void>(
        settings: settings,
        builder: (_) => const DashboardPage(),
      );
    }

    if (_authenticatedRoutes.contains(name)) {
      if (session == null) {
        return _messageRoute(
          'Please sign in to continue.',
          settings,
          actionLabel: 'Sign in',
          onAction: (context) => Navigator.of(context)
              .pushNamedAndRemoveUntil('/login', (_) => false),
        );
      }
      if (!isRouteAllowedForSession(name, session)) {
        return _messageRoute(
          'Your account does not have access to this section.',
          settings,
          actionLabel: 'Go to dashboard',
          onAction: (context) => Navigator.of(context)
              .pushNamedAndRemoveUntil('/dashboard', (_) => false),
        );
      }
    }

    final pageBuilder = _pageBuilders[name];
    if (pageBuilder == null) {
      return _messageRoute('Page not found.', settings);
    }

    if (!isRouteArgumentsValid(name, settings.arguments)) {
      return _messageRoute(
        name == '/plans/review'
            ? 'A valid plan ID is required to review a plan.'
            : 'Required page information is missing or invalid.',
        settings,
      );
    }

    final arguments = _argumentsForRoute(name, settings.arguments, session);
    final routeSettings = RouteSettings(name: name, arguments: arguments);
    return MaterialPageRoute<void>(
      settings: routeSettings,
      builder: pageBuilder,
    );
  }
}

final Map<String, WidgetBuilder> _pageBuilders = {
  '/dashboard': (_) => const DashboardPage(),
  '/onboarding': (_) => const OnboardingPage(),
  '/events': (_) => const EventListPage(),
  '/events/create': (_) => const EventFormPage(),
  '/events/details': (_) => const EventDetailsPage(),
  '/events/edit': (_) => const EventFormPage(isEditing: true),
  '/vendors/profile': (_) => const VendorProfilePage(),
  '/vendors/services': (_) => const VendorServicesPage(),
  '/vendors/availability': (_) => const VendorAvailabilityPage(),
  '/vendors/quotations': (_) => const VendorQuotationsPage(),
  '/vendors/quotations/details': (_) => const VendorQuotationDetailPage(),
  '/vendors/bookings': (_) => const VendorBookingsPage(),
  '/marketplace': (_) => const VendorMarketplacePage(),
  '/marketplace/details': (_) => const VendorMarketplaceDetailPage(),
  '/marketplace/request-quotation': (_) => const RequestQuotationPage(),
  '/quotations/mine': (_) => const MyQuotationsPage(),
  '/bookings/mine': (_) => const MyBookingsPage(),
  '/bookings/details': (_) => const BookingDetailPage(),
  '/plans/review': (context) => PlanReviewPage(
        planId: ModalRoute.of(context)!.settings.arguments as String,
      ),
};

bool isRouteAllowedForSession(
  String routeName,
  AuthResponseModel? session,
) {
  if (session == null || !session.isAuthorizedForMobile) return false;
  if (_plannerRoutes.contains(routeName)) {
    return session.roles.contains('EVENT_PLANNER');
  }
  if (_vendorRoutes.contains(routeName)) {
    return session.roles.contains('VENDOR');
  }
  return routeName == '/dashboard';
}

bool isRouteArgumentsValid(String routeName, Object? arguments) {
  switch (routeName) {
    case '/events':
    case '/marketplace':
    case '/quotations/mine':
    case '/bookings/mine':
    case '/vendors/profile':
    case '/vendors/services':
    case '/vendors/availability':
    case '/vendors/quotations':
    case '/vendors/bookings':
      return arguments == null || arguments is AuthResponseModel;
    case '/events/create':
      return arguments == null ||
          arguments is AuthResponseModel ||
          (arguments is Map && arguments['auth'] is AuthResponseModel);
    case '/events/details':
      return arguments is Map &&
          arguments['auth'] is AuthResponseModel &&
          arguments['event'] is EventModel;
    case '/events/edit':
      return arguments is Map &&
          arguments['auth'] is AuthResponseModel &&
          (arguments['event'] is EventModel ||
              _nonEmptyString(arguments['eventId']));
    case '/marketplace/details':
      return arguments is Map &&
          _nonEmptyString(arguments['vendorId']) &&
          (arguments['auth'] == null || arguments['auth'] is AuthResponseModel);
    case '/marketplace/request-quotation':
      return arguments is Map &&
          _nonEmptyString(arguments['vendorId']) &&
          (arguments['auth'] == null || arguments['auth'] is AuthResponseModel);
    case '/vendors/quotations/details':
      return arguments is Map &&
          _nonEmptyString(arguments['quotationId']) &&
          (arguments['auth'] == null || arguments['auth'] is AuthResponseModel);
    case '/bookings/details':
      return arguments is Map &&
          _nonEmptyString(arguments['bookingId']) &&
          (arguments['auth'] == null || arguments['auth'] is AuthResponseModel);
    case '/plans/review':
      return _nonEmptyString(arguments);
    default:
      return true;
  }
}

Object? _argumentsForRoute(
  String routeName,
  Object? arguments,
  AuthResponseModel? session,
) {
  if (session == null) return arguments;
  if (routeName == '/events/create' && arguments == null) return session;
  if (arguments == null &&
      (routeName == '/events' ||
          routeName == '/marketplace' ||
          routeName == '/quotations/mine' ||
          routeName == '/bookings/mine' ||
          _vendorRoutes.contains(routeName))) {
    return session;
  }

  if ((routeName == '/marketplace/details' ||
          routeName == '/marketplace/request-quotation' ||
          routeName == '/vendors/quotations/details' ||
          routeName == '/bookings/details') &&
      arguments is Map) {
    return {...arguments, 'auth': arguments['auth'] ?? session};
  }
  return arguments;
}

bool _nonEmptyString(Object? value) =>
    value is String && value.trim().isNotEmpty;

Route<dynamic> _messageRoute(
  String message,
  RouteSettings settings, {
  String? actionLabel,
  void Function(BuildContext context)? onAction,
}) {
  return MaterialPageRoute<void>(
    settings: settings,
    builder: (context) => Scaffold(
      appBar: AppBar(title: const Text('Plan It')),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(message, textAlign: TextAlign.center),
              if (actionLabel != null && onAction != null) ...[
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: () => onAction(context),
                  child: Text(actionLabel),
                ),
              ],
            ],
          ),
        ),
      ),
    ),
  );
}

class _StartupScreen extends StatelessWidget {
  const _StartupScreen();

  @override
  Widget build(BuildContext context) => Scaffold(
        backgroundColor: AppColors.canvas,
        body: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 76,
                height: 76,
                decoration: BoxDecoration(
                  color: AppColors.pastelPinkLight,
                  borderRadius: BorderRadius.circular(AppDimens.radiusLarge),
                ),
                child: const Icon(
                  Icons.event_available_rounded,
                  color: AppColors.pastelPinkText,
                  size: 38,
                ),
              ),
              const SizedBox(height: AppDimens.space16),
              Text(
                'Plan It',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: AppDimens.space20),
              const SizedBox(
                width: 24,
                height: 24,
                child: CircularProgressIndicator(strokeWidth: 2.5),
              ),
            ],
          ),
        ),
      );
}

class _StartupProblem extends StatelessWidget {
  final String message;

  const _StartupProblem({required this.message});

  @override
  Widget build(BuildContext context) => Scaffold(
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.error_outline, size: 40),
                const SizedBox(height: 16),
                const Text(
                  'Unable to start Plan It',
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 8),
                Text(message, textAlign: TextAlign.center),
                const SizedBox(height: 8),
                const Text(
                  'Check mobile/.env.local and set API_BASE_URL to your backend URL.',
                  textAlign: TextAlign.center,
                ),
              ],
            ),
          ),
        ),
      );
}
