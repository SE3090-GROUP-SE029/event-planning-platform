import 'package:flutter/material.dart';

class PastelCard extends StatelessWidget {
  final Color backgroundColor;
  final Widget child;
  final EdgeInsetsGeometry padding;
  final VoidCallback? onTap;
  final double? width;
  final double? height;

  const PastelCard({
    super.key,
    required this.backgroundColor,
    required this.child,
    this.padding = const EdgeInsets.all(20.0),
    this.onTap,
    this.width,
    this.height,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(20),
        child: Ink(
          width: width,
          height: height,
          decoration: BoxDecoration(
            color: backgroundColor,
            borderRadius: BorderRadius.circular(20),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.04),
                blurRadius: 16,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          padding: padding,
          child: child,
        ),
      ),
    );
  }
}
