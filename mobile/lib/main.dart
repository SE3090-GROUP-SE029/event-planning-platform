import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/theme/app_theme.dart';
import 'features/auth/pages/login_page.dart';
import 'features/auth/pages/register_page.dart';
import 'features/dashboard/pages/dashboard_page.dart';
import 'features/events/pages/event_details_page.dart';
import 'features/events/pages/event_form_page.dart';
import 'features/events/pages/event_list_page.dart';
import 'features/vendors/pages/vendor_availability_page.dart';
import 'features/vendors/pages/vendor_marketplace_detail_page.dart';
import 'features/vendors/pages/vendor_marketplace_page.dart';
import 'features/vendors/pages/vendor_profile_page.dart';
import 'features/vendors/pages/vendor_services_page.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  try {
    await dotenv.load(fileName: ".env.local");
  } catch (_) {
    try {
      await dotenv.load(fileName: ".env");
    } catch (_) {}
  }
  runApp(const ProviderScope(child: MyApp()));
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Plan It',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.pastelTheme,
      initialRoute: '/login',
      routes: {
        '/login': (context) => const LoginPage(),
        '/register': (context) => const RegisterPage(),
        '/dashboard': (context) => const DashboardPage(),
        '/events': (context) => const EventListPage(),
        '/events/create': (context) => const EventFormPage(),
        '/events/details': (context) => const EventDetailsPage(),
        '/events/edit': (context) => const EventFormPage(isEditing: true),
        '/vendors/profile': (context) => const VendorProfilePage(),
        '/vendors/services': (context) => const VendorServicesPage(),
        '/vendors/availability': (context) => const VendorAvailabilityPage(),
        '/marketplace': (context) => const VendorMarketplacePage(),
        '/marketplace/details': (context) =>
            const VendorMarketplaceDetailPage(),
      },
    );
  }
}
