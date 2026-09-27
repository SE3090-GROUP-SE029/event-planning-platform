import 'package:flutter/material.dart';

// Replace the bundled files and Storyset source links here when refreshing artwork.
const storysetOnboardingIllustrations = <String, String>{
  'introduction': 'assets/illustrations/team-work.png',
  'planner': 'assets/illustrations/business-plan.png',
  'vendor': 'assets/illustrations/mobile-marketing.png',
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
