import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/theme/app_colors.dart';
import 'package:mobile/core/theme/app_theme.dart';
import 'package:mobile/shared/widgets/pastel_card.dart';
import 'package:mobile/shared/widgets/black_pill_button.dart';

void main() {
  group('Pastel SaaS UI Widgets Test', () {
    testWidgets('PastelCard renders child with correct background and 20px radius', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: PastelCard(
              backgroundColor: AppColors.pastelPink,
              child: Text('Pastel Test'),
            ),
          ),
        ),
      );

      expect(find.text('Pastel Test'), findsOneWidget);
    });

    testWidgets('BlackPillButton renders label and fires callback on tap', (tester) async {
      bool pressed = false;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: BlackPillButton(
              label: '+ Add event',
              onPressed: () {
                pressed = true;
              },
            ),
          ),
        ),
      );

      expect(find.text('+ Add event'), findsOneWidget);
      await tester.tap(find.text('+ Add event'));
      expect(pressed, isTrue);
    });

    test('AppTheme.pastelTheme uses correct canvas color and M3', () {
      final theme = AppTheme.pastelTheme;
      expect(theme.useMaterial3, isTrue);
      expect(theme.scaffoldBackgroundColor, AppColors.canvas);
    });
  });
}
