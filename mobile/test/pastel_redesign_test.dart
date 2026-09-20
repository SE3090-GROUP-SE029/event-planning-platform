import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/theme/app_theme.dart';
import 'package:mobile/shared/widgets/pastel_bottom_nav_bar.dart';
import 'package:mobile/shared/widgets/pastel_card.dart';
import 'package:mobile/shared/widgets/pastel_icon_badge.dart';
import 'package:mobile/shared/widgets/pastel_list_item.dart';
import 'package:mobile/shared/widgets/pastel_pill_badge.dart';
import 'package:mobile/shared/widgets/pastel_section_header.dart';

void main() {
  testWidgets('PastelCard renders child and responds to tap',
      (tester) async {
    bool tapped = false;
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.pastelTheme,
        home: Scaffold(
          body: PastelCard(
            onTap: () => tapped = true,
            child: const Text('Test Card Content'),
          ),
        ),
      ),
    );

    expect(find.text('Test Card Content'), findsOneWidget);
    await tester.tap(find.text('Test Card Content'));
    expect(tapped, isTrue);
  });

  testWidgets('PastelIconBadge renders correctly with variant',
      (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: PastelIconBadge(
            icon: Icons.celebration_rounded,
            variant: PastelIconVariant.pink,
          ),
        ),
      ),
    );

    expect(find.byIcon(Icons.celebration_rounded), findsOneWidget);
  });

  testWidgets('PastelPillBadge renders counter and ribbon variants',
      (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: Column(
            children: [
              PastelPillBadge.counter('24'),
              PastelRibbonBanner(
                title: 'ID: 1EG4-TE5-MK72',
                subtitle: 'CONFIRMED',
              ),
            ],
          ),
        ),
      ),
    );

    expect(find.text('24'), findsOneWidget);
    expect(find.text('ID: 1EG4-TE5-MK72'), findsOneWidget);
    expect(find.text('CONFIRMED'), findsOneWidget);
  });

  testWidgets('PastelListItem renders title, subtitle, counter and handles tap',
      (tester) async {
    bool itemTapped = false;
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.pastelTheme,
        home: Scaffold(
          body: PastelListItem(
            icon: Icons.event_available_rounded,
            iconVariant: PastelIconVariant.pink,
            title: 'My Events',
            subtitle: 'Manage event list',
            countBadge: '3',
            onTap: () => itemTapped = true,
          ),
        ),
      ),
    );

    expect(find.text('My Events'), findsOneWidget);
    expect(find.text('Manage event list'), findsOneWidget);
    expect(find.text('3'), findsOneWidget);

    await tester.tap(find.text('My Events'));
    expect(itemTapped, isTrue);
  });

  testWidgets('PastelBottomNavBar triggers onTap and onCenterActionTap',
      (tester) async {
    int? selectedIndex;
    bool centerTapped = false;

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.pastelTheme,
        home: Scaffold(
          bottomNavigationBar: PastelBottomNavBar(
            currentIndex: 0,
            onTap: (index) => selectedIndex = index,
            onCenterActionTap: () => centerTapped = true,
          ),
        ),
      ),
    );

    // Tap the center action button (+)
    await tester.tap(find.byIcon(Icons.add_rounded));
    expect(centerTapped, isTrue);

    // Tap Events tab (index 1)
    await tester.tap(find.byIcon(Icons.calendar_month_outlined));
    expect(selectedIndex, 1);
  });

  testWidgets('PastelSectionHeader renders title and optional action',
      (tester) async {
    bool actionTapped = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: PastelSectionHeader(
            title: 'Workspace',
            actionLabel: 'See all',
            onActionTap: () => actionTapped = true,
          ),
        ),
      ),
    );

    expect(find.text('Workspace'), findsOneWidget);
    expect(find.text('See all'), findsOneWidget);
    await tester.tap(find.text('See all'));
    expect(actionTapped, isTrue);
  });
}
