import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

abstract interface class OnboardingStore {
  Future<bool> isComplete();
  Future<void> setComplete(bool complete);
}

class SharedPreferencesOnboardingStore implements OnboardingStore {
  static const _completionKey = 'onboarding_completed';

  @override
  Future<bool> isComplete() async {
    final preferences = await SharedPreferences.getInstance();
    return preferences.getBool(_completionKey) ?? false;
  }

  @override
  Future<void> setComplete(bool complete) async {
    final preferences = await SharedPreferences.getInstance();
    final saved = await preferences.setBool(_completionKey, complete);
    if (!saved) {
      throw StateError('Onboarding completion could not be saved.');
    }
  }
}

final onboardingStoreProvider = Provider<OnboardingStore>(
  (ref) => SharedPreferencesOnboardingStore(),
);

final onboardingCompletionProvider =
    AsyncNotifierProvider<OnboardingCompletionNotifier, bool>(
  OnboardingCompletionNotifier.new,
);

class OnboardingCompletionNotifier extends AsyncNotifier<bool> {
  OnboardingStore get _store => ref.read(onboardingStoreProvider);

  @override
  Future<bool> build() => _store.isComplete();

  Future<void> markComplete() async {
    await _store.setComplete(true);
    state = const AsyncData(true);
  }

  Future<void> reset() async {
    await _store.setComplete(false);
    state = const AsyncData(false);
  }
}
