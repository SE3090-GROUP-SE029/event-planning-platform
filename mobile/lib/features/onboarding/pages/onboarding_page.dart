import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../providers/onboarding_provider.dart';
import '../widgets/storyset_illustration.dart';

class OnboardingPage extends ConsumerStatefulWidget {
  const OnboardingPage({super.key});

  @override
  ConsumerState<OnboardingPage> createState() => _OnboardingPageState();
}

class _OnboardingPageState extends ConsumerState<OnboardingPage> {
  static const _slides = [
    _OnboardingSlide(
      illustration: 'introduction',
      imageLabel: 'A team planning and collaborating on an event',
      eyebrow: 'WELCOME TO PLAN IT',
      title: 'Make room for the moments that matter.',
      description:
          'Plan events efficiently, connect planners and vendors, and keep every detail moving together.',
      highlights: [
        'Bring event details into one workspace',
        'Keep your team and vendors in sync',
        'Simplify organization from day one',
      ],
    ),
    _OnboardingSlide(
      illustration: 'planner',
      imageLabel: 'People working together on an organized plan',
      eyebrow: 'FOR EVENT PLANNERS',
      title: 'From first idea to a thoughtful plan.',
      description:
          'Shape an event plan quickly, organize the work, and keep progress visible to everyone involved.',
      highlights: [
        'Create events, tasks, and schedules',
        'Keep budgets clear and organized',
        'Collaborate with event vendors',
      ],
    ),
    _OnboardingSlide(
      illustration: 'vendor',
      imageLabel: 'A business owner sharing services through mobile tools',
      eyebrow: 'FOR VENDORS',
      title: 'Put your services in the right place.',
      description:
          'Discover new opportunities, manage booking requests, and build visibility with event planners.',
      highlights: [
        'Showcase your services and expertise',
        'Manage requests and conversations',
        'Grow your business visibility',
      ],
    ),
  ];

  final _pageController = PageController();
  int _pageIndex = 0;
  bool _isSaving = false;

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  void _goToPage(int index) {
    _pageController.animateToPage(
      index,
      duration: const Duration(milliseconds: 280),
      curve: Curves.easeOutCubic,
    );
  }

  Future<void> _finishOnboarding() async {
    if (_isSaving) return;
    setState(() => _isSaving = true);
    try {
      await ref.read(onboardingCompletionProvider.notifier).markComplete();
      if (mounted) {
        Navigator.of(context).pushNamedAndRemoveUntil('/login', (_) => false);
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Could not save onboarding progress: $error'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isLastPage = _pageIndex == _slides.length - 1;
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(
            AppDimens.space20,
            AppDimens.space12,
            AppDimens.space20,
            AppDimens.space16,
          ),
          child: Column(
            children: [
              Row(
                children: [
                  const Icon(
                    Icons.event_available_rounded,
                    color: AppColors.pastelPinkText,
                  ),
                  const SizedBox(width: AppDimens.space8),
                  Text(
                    'Plan It',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w800,
                        ),
                  ),
                  const Spacer(),
                  TextButton(
                    onPressed: _isSaving ? null : _finishOnboarding,
                    child: const Text('Skip'),
                  ),
                ],
              ),
              Expanded(
                child: PageView.builder(
                  controller: _pageController,
                  itemCount: _slides.length,
                  onPageChanged: (index) => setState(() => _pageIndex = index),
                  itemBuilder: (context, index) =>
                      _OnboardingSlideView(slide: _slides[index]),
                ),
              ),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: List.generate(
                  _slides.length,
                  (index) => AnimatedContainer(
                    duration: const Duration(milliseconds: 180),
                    margin: const EdgeInsets.symmetric(horizontal: 4),
                    width: index == _pageIndex ? 22 : 7,
                    height: 7,
                    decoration: BoxDecoration(
                      color: index == _pageIndex
                          ? AppColors.obsidianBlack
                          : AppColors.borderMuted,
                      borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                    ),
                  ),
                ),
              ),
              const SizedBox(height: AppDimens.space16),
              Row(
                children: [
                  TextButton(
                    onPressed: _pageIndex == 0 || _isSaving
                        ? null
                        : () => _goToPage(_pageIndex - 1),
                    child: const Text('Previous'),
                  ),
                  const Spacer(),
                  SizedBox(
                    height: 48,
                    child: ElevatedButton(
                      onPressed: _isSaving
                          ? null
                          : isLastPage
                              ? _finishOnboarding
                              : () => _goToPage(_pageIndex + 1),
                      child: _isSaving
                          ? const SizedBox(
                              width: 20,
                              height: 20,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            )
                          : Text(isLastPage ? 'Get Started' : 'Next'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space12),
              const Text(
                'Illustrations by Storyset',
                style: TextStyle(
                  color: AppColors.textMuted,
                  fontSize: 11,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _OnboardingSlideView extends StatelessWidget {
  final _OnboardingSlide slide;

  const _OnboardingSlideView({required this.slide});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 560),
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(vertical: AppDimens.space12),
          child: Column(
            children: [
              SizedBox(
                height: MediaQuery.sizeOf(context).height * 0.31,
                child: StorysetIllustration(
                  name: slide.illustration,
                  semanticLabel: slide.imageLabel,
                ),
              ),
              const SizedBox(height: AppDimens.space16),
              PastelCard(
                padding: const EdgeInsets.all(AppDimens.space20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      slide.eyebrow,
                      style: const TextStyle(
                        color: AppColors.pastelPinkText,
                        fontSize: 11,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 1,
                      ),
                    ),
                    const SizedBox(height: AppDimens.space8),
                    Text(
                      slide.title,
                      style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                            fontSize: 24,
                            height: 1.16,
                          ),
                    ),
                    const SizedBox(height: AppDimens.space8),
                    Text(
                      slide.description,
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                    const SizedBox(height: AppDimens.space16),
                    ...slide.highlights.map(
                      (highlight) => Padding(
                        padding: const EdgeInsets.only(bottom: AppDimens.space8),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Icon(
                              Icons.check_circle_rounded,
                              color: AppColors.oliveRibbon,
                              size: 18,
                            ),
                            const SizedBox(width: AppDimens.space8),
                            Expanded(
                              child: Text(
                                highlight,
                                style: Theme.of(context).textTheme.bodyMedium,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _OnboardingSlide {
  final String illustration;
  final String imageLabel;
  final String eyebrow;
  final String title;
  final String description;
  final List<String> highlights;

  const _OnboardingSlide({
    required this.illustration,
    required this.imageLabel,
    required this.eyebrow,
    required this.title,
    required this.description,
    required this.highlights,
  });
}
