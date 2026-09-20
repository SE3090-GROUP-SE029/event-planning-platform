import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import 'pastel_icon_badge.dart';
import 'pastel_pill_badge.dart';

class PastelListItem extends StatelessWidget {
  final IconData icon;
  final PastelIconVariant iconVariant;
  final String title;
  final String? subtitle;
  final String? countBadge;
  final Widget? trailing;
  final VoidCallback? onTap;
  final bool showDivider;
  final EdgeInsetsGeometry padding;

  const PastelListItem({
    super.key,
    required this.icon,
    this.iconVariant = PastelIconVariant.pink,
    required this.title,
    this.subtitle,
    this.countBadge,
    this.trailing,
    this.onTap,
    this.showDivider = false,
    this.padding = const EdgeInsets.symmetric(vertical: 10, horizontal: 4),
  });

  @override
  Widget build(BuildContext context) {
    Widget itemContent = Padding(
      padding: padding,
      child: Row(
        children: [
          PastelIconBadge(
            icon: icon,
            variant: iconVariant,
            size: 44,
            iconSize: 22,
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    color: AppColors.textPrimary,
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    letterSpacing: -0.2,
                  ),
                ),
                if (subtitle != null) ...[
                  const SizedBox(height: 3),
                  Text(
                    subtitle!,
                    style: const TextStyle(
                      color: AppColors.textSecondary,
                      fontSize: 13,
                    ),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(width: 8),
          if (countBadge != null) ...[
            PastelPillBadge.counter(countBadge!),
            const SizedBox(width: 8),
          ],
          if (trailing != null)
            trailing!
          else
            const Icon(
              Icons.chevron_right_rounded,
              color: AppColors.textMuted,
              size: 22,
            ),
        ],
      ),
    );

    if (onTap != null) {
      itemContent = InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
        child: itemContent,
      );
    }

    if (showDivider) {
      return Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          itemContent,
          const Divider(
            height: 1,
            thickness: 0.8,
            color: Color(0x0C000000),
            indent: 58,
          ),
        ],
      );
    }

    return itemContent;
  }
}
