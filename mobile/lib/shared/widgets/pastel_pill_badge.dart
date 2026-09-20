import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';

enum PastelBadgeStyle {
  darkCounter,
  oliveRibbon,
  pink,
  green,
  yellow,
  blue,
  lavender,
  neutral,
}

class PastelPillBadge extends StatelessWidget {
  final String text;
  final PastelBadgeStyle style;
  final IconData? icon;
  final double fontSize;
  final EdgeInsetsGeometry? padding;

  const PastelPillBadge({
    super.key,
    required this.text,
    this.style = PastelBadgeStyle.neutral,
    this.icon,
    this.fontSize = 11.0,
    this.padding,
  });

  /// Factory constructor for dark counter badge like `[2]`, `[3]`, `[24]` in the reference
  const PastelPillBadge.counter(
    this.text, {
    super.key,
    this.fontSize = 11.0,
    this.padding = const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
  })  : style = PastelBadgeStyle.darkCounter,
        icon = null;

  @override
  Widget build(BuildContext context) {
    final colors = _getColors(style);

    return Container(
      padding: padding ?? const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: colors.background,
        borderRadius: BorderRadius.circular(AppDimens.radiusPill),
        border: colors.border != null ? Border.all(color: colors.border!) : null,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          if (icon != null) ...[
            Icon(icon, size: fontSize + 2, color: colors.foreground),
            const SizedBox(width: 4),
          ],
          Text(
            text,
            style: TextStyle(
              color: colors.foreground,
              fontSize: fontSize,
              fontWeight: FontWeight.w700,
              letterSpacing: -0.1,
            ),
          ),
        ],
      ),
    );
  }

  _BadgeColors _getColors(PastelBadgeStyle style) {
    switch (style) {
      case PastelBadgeStyle.darkCounter:
        return const _BadgeColors(
          background: AppColors.badgeDark,
          foreground: AppColors.badgeDarkText,
        );
      case PastelBadgeStyle.oliveRibbon:
        return const _BadgeColors(
          background: AppColors.oliveRibbon,
          foreground: AppColors.oliveRibbonText,
        );
      case PastelBadgeStyle.pink:
        return const _BadgeColors(
          background: AppColors.pastelPinkLight,
          foreground: AppColors.pastelPinkText,
          border: Color(0x30F9BFD8),
        );
      case PastelBadgeStyle.green:
        return const _BadgeColors(
          background: AppColors.pastelGreenLight,
          foreground: AppColors.pastelGreenText,
          border: Color(0x30C4DDB8),
        );
      case PastelBadgeStyle.yellow:
        return const _BadgeColors(
          background: AppColors.pastelYellowLight,
          foreground: AppColors.pastelYellowText,
          border: Color(0x30FEE388),
        );
      case PastelBadgeStyle.blue:
        return const _BadgeColors(
          background: AppColors.pastelBlueLight,
          foreground: AppColors.pastelBlueText,
          border: Color(0x30BCD8F0),
        );
      case PastelBadgeStyle.lavender:
        return const _BadgeColors(
          background: AppColors.pastelLavenderLight,
          foreground: AppColors.pastelLavenderText,
          border: Color(0x30E5D4F7),
        );
      case PastelBadgeStyle.neutral:
        return const _BadgeColors(
          background: AppColors.surfaceMuted,
          foreground: AppColors.textPrimary,
          border: AppColors.borderSubtle,
        );
    }
  }
}

class _BadgeColors {
  final Color background;
  final Color foreground;
  final Color? border;
  const _BadgeColors({
    required this.background,
    required this.foreground,
    this.border,
  });
}

/// A full-width ribbon inspired by the olive status pill in the reference design
class PastelRibbonBanner extends StatelessWidget {
  final String title;
  final String? subtitle;
  final Widget? trailing;
  final Color backgroundColor;
  final Color textColor;
  final IconData? icon;

  const PastelRibbonBanner({
    super.key,
    required this.title,
    this.subtitle,
    this.trailing,
    this.backgroundColor = AppColors.oliveRibbon,
    this.textColor = Colors.white,
    this.icon,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(AppDimens.radiusLarge),
      ),
      child: Row(
        children: [
          if (icon != null) ...[
            Icon(icon, size: 18, color: textColor.withValues(alpha: 0.9)),
            const SizedBox(width: 8),
          ],
          Expanded(
            child: Text(
              title,
              style: TextStyle(
                color: textColor,
                fontSize: 13,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.2,
              ),
              overflow: TextOverflow.ellipsis,
            ),
          ),
          if (subtitle != null) ...[
            Text(
              subtitle!,
              style: TextStyle(
                color: textColor.withValues(alpha: 0.9),
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
          ],
          if (trailing != null) trailing!,
        ],
      ),
    );
  }
}
