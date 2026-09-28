import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../features/auth/models/auth_response_model.dart';

class PastelBottomNavItem {
  final IconData icon;
  final IconData inactiveIcon;
  final String label;
  final String? routeName;

  const PastelBottomNavItem({
    required this.icon,
    IconData? inactiveIcon,
    required this.label,
    this.routeName,
  }) : inactiveIcon = inactiveIcon ?? icon;
}

class PastelBottomNavBar extends StatelessWidget {
  final int currentIndex;
  final ValueChanged<int> onTap;
  final VoidCallback? onCenterActionTap;
  final List<PastelBottomNavItem>? items;
  final IconData centerIcon;
  final Color centerColor;
  final String? centerTooltip;

  const PastelBottomNavBar({
    super.key,
    required this.currentIndex,
    required this.onTap,
    this.onCenterActionTap,
    this.items,
    this.centerIcon = Icons.add_rounded,
    this.centerColor = AppColors.pastelPink,
    this.centerTooltip,
  });

  /// Factory constructor to build role-appropriate navigation bar for Planners or Vendors
  static Widget roleBased({
    Key? key,
    required BuildContext context,
    required AuthResponseModel? session,
    required int currentIndex,
    VoidCallback? onCenterActionTap,
  }) {
    final isVendor = session?.roles.contains('VENDOR') ?? false;

    if (isVendor) {
      final vendorItems = [
        const PastelBottomNavItem(
          icon: Icons.home_rounded,
          inactiveIcon: Icons.home_outlined,
          label: 'Home',
          routeName: '/dashboard',
        ),
        const PastelBottomNavItem(
          icon: Icons.handyman_rounded,
          inactiveIcon: Icons.handyman_outlined,
          label: 'Services',
          routeName: '/vendors/services',
        ),
        const PastelBottomNavItem(
          icon: Icons.request_quote_rounded,
          inactiveIcon: Icons.request_quote_outlined,
          label: 'Requests',
          routeName: '/vendors/quotations',
        ),
        const PastelBottomNavItem(
          icon: Icons.storefront_rounded,
          inactiveIcon: Icons.storefront_outlined,
          label: 'Profile',
          routeName: '/vendors/profile',
        ),
      ];

      return PastelBottomNavBar(
        key: key,
        currentIndex: currentIndex,
        items: vendorItems,
        centerIcon: Icons.add_rounded,
        centerColor: AppColors.pastelBlue,
        centerTooltip: 'Manage services',
        onTap: (index) {
          if (index == currentIndex) return;
          final target = vendorItems[index].routeName;
          if (target != null) {
            _navigate(context, target, session);
          }
        },
        onCenterActionTap: onCenterActionTap ??
            () => _navigate(context, '/vendors/services', session),
      );
    }

    // Default: Event Planner
    final plannerItems = [
      const PastelBottomNavItem(
        icon: Icons.home_rounded,
        inactiveIcon: Icons.home_outlined,
        label: 'Home',
        routeName: '/dashboard',
      ),
      const PastelBottomNavItem(
        icon: Icons.calendar_month_rounded,
        inactiveIcon: Icons.calendar_month_outlined,
        label: 'Events',
        routeName: '/events',
      ),
      const PastelBottomNavItem(
        icon: Icons.storefront_rounded,
        inactiveIcon: Icons.storefront_outlined,
        label: 'Vendors',
        routeName: '/marketplace',
      ),
      const PastelBottomNavItem(
        icon: Icons.receipt_long_rounded,
        inactiveIcon: Icons.receipt_long_outlined,
        label: 'Quotes',
        routeName: '/quotations/mine',
      ),
    ];

    return PastelBottomNavBar(
      key: key,
      currentIndex: currentIndex,
      items: plannerItems,
      centerIcon: Icons.add_rounded,
      centerColor: AppColors.pastelPink,
      centerTooltip: 'Create event',
      onTap: (index) {
        if (index == currentIndex) return;
        final target = plannerItems[index].routeName;
        if (target != null) {
          _navigate(context, target, session);
        }
      },
      onCenterActionTap: onCenterActionTap ??
          () => Navigator.of(context)
              .pushNamed('/events/create', arguments: session),
    );
  }

  static void _navigate(
    BuildContext context,
    String targetRoute,
    AuthResponseModel? session,
  ) {
    final currentRouteName = ModalRoute.of(context)?.settings.name;
    if (currentRouteName == targetRoute) return;

    if (targetRoute == '/dashboard') {
      Navigator.of(context).pushNamedAndRemoveUntil(
        '/dashboard',
        (_) => false,
        arguments: session,
      );
    } else {
      Navigator.of(context).pushNamedAndRemoveUntil(
        targetRoute,
        (route) => route.settings.name == '/dashboard' || route.isFirst,
        arguments: session,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final navItems = items ??
        const [
          PastelBottomNavItem(
            icon: Icons.home_rounded,
            inactiveIcon: Icons.home_outlined,
            label: 'Home',
          ),
          PastelBottomNavItem(
            icon: Icons.calendar_month_rounded,
            inactiveIcon: Icons.calendar_month_outlined,
            label: 'Events',
          ),
          PastelBottomNavItem(
            icon: Icons.storefront_rounded,
            inactiveIcon: Icons.storefront_outlined,
            label: 'Vendors',
          ),
          PastelBottomNavItem(
            icon: Icons.person_rounded,
            inactiveIcon: Icons.person_outline_rounded,
            label: 'Profile',
          ),
        ];

    final leftItems = navItems.take(2).toList();
    final rightItems = navItems.skip(2).take(2).toList();

    return Container(
      margin: const EdgeInsets.fromLTRB(16, 0, 16, 16),
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: AppColors.obsidianNav,
        borderRadius: BorderRadius.circular(36),
        boxShadow: AppDimens.floatingShadow,
      ),
      child: SafeArea(
        top: false,
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceAround,
          children: [
            for (var i = 0; i < leftItems.length; i++)
              _buildNavButton(
                index: i,
                item: leftItems[i],
              ),
            // Center Action Button
            GestureDetector(
              onTap: onCenterActionTap,
              child: Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: centerColor,
                  shape: BoxShape.circle,
                  boxShadow: [
                    BoxShadow(
                      color: centerColor.withValues(alpha: 0.35),
                      blurRadius: 10,
                      offset: const Offset(0, 3),
                    ),
                  ],
                ),
                child: Icon(
                  centerIcon,
                  color: AppColors.obsidianBlack,
                  size: 28,
                ),
              ),
            ),
            for (var i = 0; i < rightItems.length; i++)
              _buildNavButton(
                index: leftItems.length + i,
                item: rightItems[i],
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildNavButton({
    required int index,
    required PastelBottomNavItem item,
  }) {
    final isSelected = currentIndex == index;

    return InkWell(
      onTap: () => onTap(index),
      borderRadius: BorderRadius.circular(20),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              isSelected ? item.icon : item.inactiveIcon,
              color: isSelected ? centerColor : Colors.white70,
              size: 22,
            ),
            const SizedBox(height: 3),
            Text(
              item.label,
              style: TextStyle(
                color: isSelected ? Colors.white : Colors.white60,
                fontSize: 10,
                fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
              ),
            ),
            const SizedBox(height: 2),
            Container(
              width: 4,
              height: 4,
              decoration: BoxDecoration(
                color: isSelected ? centerColor : Colors.transparent,
                shape: BoxShape.circle,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
