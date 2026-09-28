import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/onboarding/providers/onboarding_provider.dart';

void main() {
  test('onboarding completion is persisted and can be reset', () async {
    final store = _MemoryOnboardingStore();

    expect(await store.isComplete(), isFalse);
    await store.setComplete(true);
    expect(await store.isComplete(), isTrue);
    await store.setComplete(false);
    expect(await store.isComplete(), isFalse);
  });
}

class _MemoryOnboardingStore implements OnboardingStore {
  bool _complete = false;

  @override
  Future<bool> isComplete() async => _complete;

  @override
  Future<void> setComplete(bool complete) async {
    _complete = complete;
  }
}
