import 'package:flutter/material.dart';

// Replace the bundled files and Storyset source links here when refreshing artwork.
const storysetOnboardingIllustrations = <String, String>{
  'introduction': 'assets/illustrations/planit-img.png',
  'planner': 'assets/illustrations/planner-img.png',
  'vendor': 'assets/illustrations/vendor-img.png',
};

class StorysetIllustration extends StatelessWidget {
  final String name;
  final String semanticLabel;

  const StorysetIllustration({
    super.key,
    required this.name,
    required this.semanticLabel,
  });

  @override
  Widget build(BuildContext context) {
    final asset = storysetOnboardingIllustrations[name];
    if (asset == null) {
      throw ArgumentError.value(name, 'name', 'Unknown onboarding illustration');
    }

    return Image.asset(
      asset,
      semanticLabel: semanticLabel,
      fit: BoxFit.contain,
    );
  }
}
