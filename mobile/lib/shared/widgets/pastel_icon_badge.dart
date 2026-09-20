import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';

enum PastelIconVariant {
  pink,
  yellow,
  green,
  blue,
  lavender,
  olive,
  obsidian,
}

class PastelIconBadge extends StatelessWidget {
  final IconData icon;
  final PastelIconVariant variant;
  final double size;
  final double iconSize;
  final bool isCircle;

  const PastelIconBadge({
    super.key,
    required this.icon,
    this.variant = PastelIconVariant.pink,
    this.size = 46.0,
    this.iconSize = 22.0,
    this.isCircle = true,
  });

  @override
  Widget build(BuildContext context) {
    final colors = _getColors(variant);

    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: colors.background,
        shape: isCircle ? BoxShape.circle : BoxShape.rectangle,
        borderRadius: isCircle ? null : BorderRadius.circular(16),
      ),
      alignment: Alignment.center,
      child: Icon(
        icon,
        size: iconSize,
        color: colors.foreground,
      ),
    );
  }

  _VariantColor _getColors(PastelIconVariant variant) {
    switch (variant) {
      case PastelIconVariant.pink:
        return const _VariantColor(
          background: AppColors.pastelPink,
          foreground: AppColors.pastelPinkText,
        );
      case PastelIconVariant.yellow:
        return const _VariantColor(
          background: AppColors.pastelYellow,
          foreground: AppColors.pastelYellowText,
        );
      case PastelIconVariant.green:
        return const _VariantColor(
          background: AppColors.pastelGreen,
          foreground: AppColors.pastelGreenText,
        );
      case PastelIconVariant.blue:
        return const _VariantColor(
          background: AppColors.pastelBlue,
          foreground: AppColors.pastelBlueText,
        );
      case PastelIconVariant.lavender:
        return const _VariantColor(
          background: AppColors.pastelLavender,
          foreground: AppColors.pastelLavenderText,
        );
      case PastelIconVariant.olive:
        return const _VariantColor(
          background: AppColors.oliveRibbonLight,
          foreground: AppColors.oliveRibbon,
        );
      case PastelIconVariant.obsidian:
        return const _VariantColor(
          background: AppColors.obsidianBlack,
          foreground: Colors.white,
        );
    }
  }
}

class _VariantColor {
  final Color background;
  final Color foreground;
  const _VariantColor({required this.background, required this.foreground});
}
