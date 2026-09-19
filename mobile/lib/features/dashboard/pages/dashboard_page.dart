import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/providers/auth_providers.dart';

class DashboardPage extends ConsumerWidget {
  const DashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(currentUserProvider);
    final roles = session?.roles ?? const <String>[];
    final isVendor = roles.contains('VENDOR');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Plan It'),
        actions: [
          IconButton(
            tooltip: 'Sign out',
            onPressed: () async {
              await ref.read(authNotifierProvider.notifier).logout();
              if (context.mounted) {
                Navigator.of(context).pushNamedAndRemoveUntil('/login', (_) => false);
              }
            },
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Text(
            'Welcome back',
            style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Text(
            session?.email ?? 'Your account',
            style: Theme.of(context).textTheme.bodyMedium,
          ),
          const SizedBox(height: 24),
          _HomeAction(
            icon: Icons.event_outlined,
            title: 'Events',
            description: 'Create, view, update, and delete your events.',
            onPressed: () => Navigator.of(context).pushNamed('/events', arguments: session),
          ),
          if (isVendor) ...[
            const SizedBox(height: 12),
            _HomeAction(
              icon: Icons.storefront_outlined,
              title: 'Vendor profile',
              description: 'Manage the vendor profile linked to your account.',
              onPressed: () => Navigator.of(context).pushNamed('/vendors/profile', arguments: session),
            ),
          ],
        ],
      ),
    );
  }
}

class _HomeAction extends StatelessWidget {
  const _HomeAction({
    required this.icon,
    required this.title,
    required this.description,
    required this.onPressed,
  });

  final IconData icon;
  final String title;
  final String description;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Icon(icon, size: 32),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(title, style: const TextStyle(fontWeight: FontWeight.bold)),
                  const SizedBox(height: 4),
                  Text(description),
                ],
              ),
            ),
            const SizedBox(width: 8),
            IconButton(onPressed: onPressed, icon: const Icon(Icons.arrow_forward)),
          ],
        ),
      ),
    );
  }
}
