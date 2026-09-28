# Plan It mobile app

The Flutter app uses named routes, Riverpod state, secure storage for authentication sessions, and SharedPreferences for onboarding completion.

## Startup and onboarding

On launch, the branded startup screen remains visible while the existing auth session and the `onboarding_completed` preference load. A valid authenticated session opens the dashboard. A new user sees the three onboarding slides; after skipping or completing them, the app opens sign in and remembers that onboarding was completed. A returning signed-out user goes directly to sign in.

Signing out clears the secure auth session and resets `onboarding_completed` to `false`, so onboarding appears next. This preference also survives an app restart.

## Illustrations

Onboarding uses local Storyset Amico PNG assets in `assets/illustrations/`, so illustrations work offline. Sources:

- [Team work](https://storyset.com/illustration/team-work/amico)
- [Business plan](https://storyset.com/illustration/business-plan/amico)
- [Mobile marketing](https://storyset.com/illustration/mobile-marketing/amico)

The onboarding screen displays Storyset attribution. See `lib/features/onboarding/widgets/storyset_illustration.dart` to replace the local image mappings.

## Local development

Configure `API_BASE_URL` in `mobile/.env.local`, then run:

```sh
flutter pub get
flutter run
flutter test
```
