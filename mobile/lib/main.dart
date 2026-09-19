import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/api/dio_client.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/api/auth_remote_datasource.dart';
import 'features/auth/api/auth_repository.dart';
import 'features/auth/bloc/auth_bloc.dart';
import 'features/auth/pages/login_page.dart';
import 'features/auth/pages/register_page.dart';
import 'features/dashboard/pages/dashboard_page.dart';
import 'features/test/pages/test_screen.dart';
import 'features/events/pages/event_details_page.dart';
import 'features/events/pages/event_form_page.dart';
import 'features/events/pages/event_list_page.dart';

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
    final dioClient = DioClient();
    final authRemoteDataSource = AuthRemoteDataSourceImpl(dio: dioClient.dio);
    final authRepository =
        AuthRepository(remoteDataSource: authRemoteDataSource);

    return MultiRepositoryProvider(
      providers: [
        RepositoryProvider<AuthRepository>.value(value: authRepository),
      ],
      child: MultiBlocProvider(
        providers: [
          BlocProvider<AuthBloc>(
            create: (context) => AuthBloc(authRepository: authRepository),
          ),
        ],
        child: MaterialApp(
          title: 'Plan It',
          debugShowCheckedModeBanner: false,
          theme: AppTheme.pastelTheme,
          initialRoute: '/login',
          routes: {
            '/login': (context) => const LoginPage(),
            '/register': (context) => const RegisterPage(),
            '/dashboard': (context) => const DashboardPage(),
            '/test': (context) => const TestScreen(),
            '/events': (context) => const EventListPage(),
            '/events/create': (context) => const EventFormPage(),
            '/events/details': (context) => const EventDetailsPage(),
            '/events/edit': (context) => const EventFormPage(isEditing: true),
          },
        ),
      ),
    );
  }
}
