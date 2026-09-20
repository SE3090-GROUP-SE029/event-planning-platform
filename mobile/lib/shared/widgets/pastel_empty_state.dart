import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import 'pastel_icon_badge.dart';

class PastelEmptyState extends StatelessWidget {
  final IconData icon;
  final PastelIconVariant iconVariant;
  final String title;
  final String description;
  final String? actionLabel;
  final VoidCallback? onActionTap;

  const PastelEmptyState({
    super.key,
    required this.icon,
    this.iconVariant = PastelIconVariant.pink,
    required this.title,
    required this.description,
    this.actionLabel,
    this.onActionTap,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 48),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          mainAxisSize: MainAxisSize.min,
          children: [
            PastelIconBadge(
              icon: icon,
              variant: iconVariant,
              size: 72,
              iconSize: 34,
              isCircle: false,
            ),
            const SizedBox(height: AppDimens.space20),
            Text(
              title,
              textAlign: TextAlign.center,
              style: const TextStyle(
                color: AppColors.textPrimary,
                fontSize: 18,
                fontWeight: FontWeight.w800,
                letterSpacing: -0.3,
              ),
            ),
            const SizedBox(height: AppDimens.space8),
            Text(
              description,
              textAlign: TextAlign.center,
              style: const TextStyle(
                color: AppColors.textSecondary,
                fontSize: 14,
                height: 1.4,
              ),
            ),
            if (actionLabel != null && onActionTap != null) ...[
              const SizedBox(height: AppDimens.space20),
              ElevatedButton(
                onPressed: onActionTap,
                child: Text(actionLabel!),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
