import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';

class PastelCard extends StatelessWidget {
  final Widget child;
  final EdgeInsetsGeometry padding;
  final VoidCallback? onTap;
  final Color? backgroundColor;
  final Border? border;
  final double? borderRadius;
  final List<BoxShadow>? customShadow;

  const PastelCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(AppDimens.space20),
    this.onTap,
    this.backgroundColor,
    this.border,
    this.borderRadius,
    this.customShadow,
  });

  @override
  Widget build(BuildContext context) {
    final radius = BorderRadius.circular(borderRadius ?? AppDimens.radiusCard);

    Widget content = Container(
      padding: padding,
      decoration: BoxDecoration(
        color: backgroundColor ?? AppColors.surfacePure,
        borderRadius: radius,
        border: border ?? Border.all(color: AppColors.borderSubtle),
        boxShadow: customShadow ?? AppDimens.cardShadow,
      ),
      child: child,
    );

    if (onTap != null) {
      return Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: onTap,
          borderRadius: radius,
          child: content,
        ),
      );
    }

    return content;
  }
}
