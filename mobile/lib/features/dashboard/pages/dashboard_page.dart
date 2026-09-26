import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_bottom_nav_bar.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_list_item.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/providers/auth_providers.dart';
import '../../vendors/widgets/vendor_dashboard_overview.dart';

class DashboardPage extends ConsumerWidget {
  const DashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(currentUserProvider);
    final roles = session?.roles ?? const <String>[];
    final isVendor = roles.contains('VENDOR');
    final isEventPlanner = roles.contains('EVENT_PLANNER');
    final email = session?.email ?? 'User';
    final name = email.split('@').first;
    final displayName = name.isNotEmpty
        ? '${name[0].toUpperCase()}${name.substring(1)}'
        : 'Planner';

    return Scaffold(
      backgroundColor: AppColors.canvas,
      bottomNavigationBar: PastelBottomNavBar(
        currentIndex: 0,
        onTap: (index) {
          if (index == 1) {
            Navigator.of(context).pushNamed('/events', arguments: session);
          } else if (index == 2 && isVendor) {
            Navigator.of(context)
                .pushNamed('/vendors/profile', arguments: session);
          }
        },
        onCenterActionTap: () {
          Navigator.of(context).pushNamed('/events/create', arguments: session);
        },
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(
            AppDimens.space20,
            AppDimens.space14,
            AppDimens.space20,
            AppDimens.space28,
          ),
          children: [
            // Top Bar
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Plan It',
                    style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                          fontWeight: FontWeight.w800,
                          letterSpacing: -0.8,
                        ),
                  ),
                ),
                InkWell(
                  onTap: () async {
                    await ref.read(authNotifierProvider.notifier).logout();
                    if (context.mounted) {
                      Navigator.of(context)
                          .pushNamedAndRemoveUntil('/login', (_) => false);
                    }
                  },
                  borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
                  child: Container(
                    width: 42,
                    height: 42,
                    decoration: BoxDecoration(
                      color: AppColors.surfacePure,
                      borderRadius:
                          BorderRadius.circular(AppDimens.radiusMedium),
                      border: Border.all(color: AppColors.borderSubtle),
                    ),
                    child: const Icon(
                      Icons.logout_rounded,
                      size: 20,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppDimens.space18),

            // Profile Header Card inspired directly by reference
            PastelCard(
              padding: const EdgeInsets.all(AppDimens.space20),
              child: Column(
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.center,
                    children: [
                      // Squircle Avatar Container
                      Container(
                        width: 72,
                        height: 72,
                        decoration: BoxDecoration(
                          color: AppColors.pastelPinkLight,
                          borderRadius: BorderRadius.circular(22),
                          border: Border.all(
                            color: const Color(0x20F9BFD8),
                            width: 1.5,
                          ),
                        ),
                        child: const Icon(
                          Icons.person_rounded,
                          color: AppColors.pastelPinkText,
                          size: 38,
                        ),
                      ),
                      const SizedBox(width: AppDimens.space16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              displayName,
                              style: const TextStyle(
                                color: AppColors.textPrimary,
                                fontSize: 22,
                                fontWeight: FontWeight.w800,
                                letterSpacing: -0.5,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              email,
                              style: const TextStyle(
                                color: AppColors.textSecondary,
                                fontSize: 13,
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                            const SizedBox(height: AppDimens.space8),
                            // Quick metadata row
                            Row(
                              children: [
                                _buildMetadataItem(
                                  label: 'ROLE',
                                  value: isVendor ? 'Vendor' : 'Planner',
                                ),
                                const SizedBox(width: AppDimens.space16),
                                _buildMetadataItem(
                                  label: 'ACCESS',
                                  value: 'Active',
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: AppDimens.space16),

                  // Olive Ribbon Pill (inspired by reference olive ribbon)
                  PastelRibbonBanner(
                    icon: Icons.shield_outlined,
                    title: session?.userId.isNotEmpty == true
                        ? 'ID: ${session!.userId.substring(0, session.userId.length > 12 ? 12 : session.userId.length)}'
                        : 'Plan It Member',
                    subtitle: isVendor ? 'VENDOR ACCESS' : 'PLANNER ACCESS',
                  ),
                ],
              ),
            ),

            if (isVendor && session != null)
              VendorDashboardOverview(session: session),

            // Section: Workspace
            const PastelSectionHeader(title: 'Workspace'),

            // Card Group containing items (matching reference "Clinical profile" card)
            PastelCard(
              padding: const EdgeInsets.symmetric(
                horizontal: AppDimens.space16,
                vertical: AppDimens.space8,
              ),
              child: Column(
                children: [
                  PastelListItem(
                    icon: Icons.event_available_rounded,
                    iconVariant: PastelIconVariant.pink,
                    title: 'Events',
                    subtitle: 'Create, review, edit, and manage your event plans',
                    showDivider: true,
                    onTap: () => Navigator.of(context)
                        .pushNamed('/events', arguments: session),
                  ),
                  if (isEventPlanner) ...[
                    PastelListItem(
                      icon: Icons.store_mall_directory_outlined,
                      iconVariant: PastelIconVariant.olive,
                      title: 'Vendor Marketplace',
                      subtitle:
                          'Browse approved vendors, services, and prices',
                      showDivider: true,
                      onTap: () => Navigator.of(context)
                          .pushNamed('/marketplace', arguments: session),
                    ),
                    PastelListItem(
                      icon: Icons.receipt_long_outlined,
                      iconVariant: PastelIconVariant.blue,
                      title: 'My quotations',
                      subtitle: 'Track pending and responded quotation requests',
                      showDivider: true,
                      onTap: () => Navigator.of(context)
                          .pushNamed('/quotations/mine', arguments: session),
                    ),
                  ],
                  if (isVendor) ...[
                    PastelListItem(
                      icon: Icons.storefront_rounded,
                      iconVariant: PastelIconVariant.olive,
                      title: 'Vendor profile',
                      subtitle:
                          'Update the business profile linked to your account',
                      showDivider: true,
                      onTap: () => Navigator.of(context)
                          .pushNamed('/vendors/profile', arguments: session),
                    ),
                    PastelListItem(
                      icon: Icons.handyman_outlined,
                      iconVariant: PastelIconVariant.blue,
                      title: 'Vendor services',
                      subtitle: 'Add, edit, and remove the services you offer',
                      showDivider: true,
                      onTap: () => Navigator.of(context)
                          .pushNamed('/vendors/services', arguments: session),
                    ),
                    PastelListItem(
                      icon: Icons.schedule_outlined,
                      iconVariant: PastelIconVariant.pink,
                      title: 'Vendor availability',
                      subtitle:
                          'Set when your business is available for events',
                      showDivider: true,
                      onTap: () => Navigator.of(context).pushNamed(
                        '/vendors/availability',
                        arguments: session,
                      ),
                    ),
                    PastelListItem(
                      icon: Icons.request_quote_outlined,
                      iconVariant: PastelIconVariant.yellow,
                      title: 'Quotation requests',
                      subtitle: 'Review and respond to planner quotation requests',
                      showDivider: true,
                      onTap: () => Navigator.of(context).pushNamed(
                        '/vendors/quotations',
                        arguments: session,
                      ),
                    ),
                  ],
                  PastelListItem(
                    icon: Icons.add_circle_outline_rounded,
                    iconVariant: PastelIconVariant.yellow,
                    title: 'Plan new event',
                    subtitle: 'Start a new event planning journey',
                    showDivider: false,
                    onTap: () => Navigator.of(context)
                        .pushNamed('/events/create', arguments: session),
                  ),
                ],
              ),
            ),

            const SizedBox(height: AppDimens.space12),

            // Section: Account & Settings
            const PastelSectionHeader(title: 'Settings'),

            PastelCard(
              padding: const EdgeInsets.symmetric(
                horizontal: AppDimens.space16,
                vertical: AppDimens.space8,
              ),
              child: Column(
                children: [
                  if (isVendor)
                    const PastelListItem(
                      icon: Icons.security_rounded,
                      iconVariant: PastelIconVariant.blue,
                      title: 'Account status',
                      subtitle: 'Verified vendor permissions',
                      countBadge: 'Active',
                      showDivider: true,
                    )
                  else
                    const PastelListItem(
                      icon: Icons.security_rounded,
                      iconVariant: PastelIconVariant.blue,
                      title: 'Account status',
                      subtitle: 'Verified planner permissions',
                      countBadge: 'Active',
                      showDivider: true,
                    ),
                  const PastelListItem(
                    icon: Icons.help_outline_rounded,
                    iconVariant: PastelIconVariant.lavender,
                    title: 'Help & support',
                    subtitle: 'Guidance and platform resources',
                    showDivider: false,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMetadataItem({required String label, required String value}) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          value,
          style: const TextStyle(
            color: AppColors.textPrimary,
            fontSize: 13,
            fontWeight: FontWeight.w700,
          ),
        ),
        Text(
          label,
          style: const TextStyle(
            color: AppColors.textMuted,
            fontSize: 10,
            fontWeight: FontWeight.w600,
            letterSpacing: 0.8,
          ),
        ),
      ],
    );
  }
}
