import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';

class BlackPillButton extends StatelessWidget {
  final String label;
  final IconData? icon;
  final VoidCallback onPressed;
  final bool isFullWidth;
  final EdgeInsetsGeometry padding;

  const BlackPillButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.icon,
    this.isFullWidth = false,
    this.padding = const EdgeInsets.symmetric(horizontal: 22, vertical: 13),
  });

  @override
  Widget build(BuildContext context) {
    final button = ElevatedButton(
      style: ElevatedButton.styleFrom(
        backgroundColor: AppColors.obsidianBlack,
        foregroundColor: Colors.white,
        shape: const StadiumBorder(),
        elevation: 0,
        padding: padding,
      ),
      onPressed: onPressed,
      child: Row(
        mainAxisSize: isFullWidth ? MainAxisSize.max : MainAxisSize.min,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 17, color: Colors.white),
            const SizedBox(width: 8),
          ],
          Text(
            label,
            style: const TextStyle(
              fontSize: 13.5,
              fontWeight: FontWeight.w600,
              letterSpacing: 0.2,
            ),
          ),
        ],
      ),
    );

    return isFullWidth ? SizedBox(width: double.infinity, child: button) : button;
  }
}
