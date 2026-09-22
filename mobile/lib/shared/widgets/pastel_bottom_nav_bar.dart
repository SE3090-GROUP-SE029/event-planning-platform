import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';

class PastelBottomNavItem {
  final IconData icon;
  final IconData? activeIcon;
  final String label;

  const PastelBottomNavItem({
    required this.icon,
    this.activeIcon,
    required this.label,
  });
}

class PastelBottomNavBar extends StatelessWidget {
  final int currentIndex;
  final ValueChanged<int> onTap;
  final VoidCallback? onCenterActionTap;
  final List<PastelBottomNavItem>? items;

  const PastelBottomNavBar({
    super.key,
    required this.currentIndex,
    required this.onTap,
    this.onCenterActionTap,
    this.items,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.fromLTRB(16, 0, 16, 16),
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
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
            // Left Item 0: Home / Dashboard
            _buildNavButton(
              index: 0,
              icon: Icons.home_rounded,
              inactiveIcon: Icons.home_outlined,
              label: 'Home',
            ),

            // Left Item 1: Events
            _buildNavButton(
              index: 1,
              icon: Icons.calendar_month_rounded,
              inactiveIcon: Icons.calendar_month_outlined,
              label: 'Events',
            ),

            // Center Action Button (Pastel Pink with +)
            GestureDetector(
              onTap: onCenterActionTap,
              child: Container(
                width: 48,
                height: 48,
                decoration: const BoxDecoration(
                  color: AppColors.pastelPink,
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.add_rounded,
                  color: AppColors.obsidianBlack,
                  size: 28,
                ),
              ),
            ),

            // Right Item 2: Vendors
            _buildNavButton(
              index: 2,
              icon: Icons.storefront_rounded,
              inactiveIcon: Icons.storefront_outlined,
              label: 'Vendors',
            ),

            // Right Item 3: Profile / Account
            _buildNavButton(
              index: 3,
              icon: Icons.person_rounded,
              inactiveIcon: Icons.person_outline_rounded,
              label: 'Profile',
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildNavButton({
    required int index,
    required IconData icon,
    required IconData inactiveIcon,
    required String label,
  }) {
    final isSelected = currentIndex == index;

    return InkWell(
      onTap: () => onTap(index),
      borderRadius: BorderRadius.circular(20),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              isSelected ? icon : inactiveIcon,
              color: isSelected ? AppColors.pastelPink : Colors.white70,
              size: 24,
            ),
            const SizedBox(height: 4),
            Container(
              width: 4,
              height: 4,
              decoration: BoxDecoration(
                color: isSelected ? AppColors.pastelPink : Colors.transparent,
                shape: BoxShape.circle,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
