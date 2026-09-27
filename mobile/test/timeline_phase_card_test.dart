import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/theme/app_colors.dart';
import 'package:mobile/features/plans/widgets/timeline_phase_card.dart';

void main() {
  group('TimelinePhaseItem parsing', () {
    test('parses AI timing:description string accurately', () {
      final item = TimelinePhaseItem.fromEntry(
        'Discovery and Core Vendor Procurement',
        'February 2026 – June 2026: Complete discovery and core vendor procurement for the event.',
      );

      expect(item.title, 'Discovery and Core Vendor Procurement');
      expect(item.dateRange, 'February 2026 – June 2026');
      expect(item.description,
          'Complete discovery and core vendor procurement for the event.');
    });

    test('parses newline delimited timing and description', () {
      final item = TimelinePhaseItem.fromEntry(
        'Venue Selection',
        'Jan 1, 2027 – Jan 27, 2027\nFinalize venue contract and menu selections.',
      );

      expect(item.title, 'Venue Selection');
      expect(item.dateRange, 'Jan 1, 2027 – Jan 27, 2027');
      expect(item.description,
          'Finalize venue contract and menu selections.');
    });

    test('handles timing with clock times containing colons', () {
      final item = TimelinePhaseItem.fromEntry(
        'Opening Keynote',
        '10:00 AM – 11:30 AM: Welcoming remarks and keynote address.',
      );

      expect(item.title, 'Opening Keynote');
      expect(item.dateRange, '10:00 AM – 11:30 AM');
      expect(item.description, 'Welcoming remarks and keynote address.');
    });

    test('handles timing-only value like "2 hours before event"', () {
      final item = TimelinePhaseItem.fromEntry(
        'Setup',
        '2 hours before event',
      );

      expect(item.title, 'Setup');
      expect(item.dateRange, '2 hours before event');
      expect(item.description, isNull);
    });

    test('handles inverted key as date and value as title', () {
      final item = TimelinePhaseItem.fromEntry(
        'February 2026 – June 2026',
        'Vendor Procurement & Contracting',
      );

      expect(item.title, 'Vendor Procurement & Contracting');
      expect(item.dateRange, 'February 2026 – June 2026');
      expect(item.description, isNull);
    });

    test('handles description-only value without timing', () {
      final item = TimelinePhaseItem.fromEntry(
        'Final Review',
        'Conduct a comprehensive walkthrough with all contracted vendors and client.',
      );

      expect(item.title, 'Final Review');
      expect(item.dateRange, isNull);
      expect(item.description,
          'Conduct a comprehensive walkthrough with all contracted vendors and client.');
    });
  });

  group('TimelinePhaseCard Widget & Responsiveness', () {
    testWidgets('renders Title, Date Chip, and Description in vertical stack',
        (tester) async {
      const item = TimelinePhaseItem(
        title: 'Discovery and Core Vendor Procurement',
        dateRange: 'February 2026 – June 2026',
        description:
            'Complete discovery and core vendor procurement for the event.',
      );

      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: TimelinePhaseCard(item: item),
          ),
        ),
      );

      // Verify title is rendered
      final titleFinder =
          find.text('Discovery and Core Vendor Procurement');
      expect(titleFinder, findsOneWidget);
      final Text titleText = tester.widget(titleFinder);
      expect(titleText.style?.fontWeight, FontWeight.w700);
      expect(titleText.style?.color, AppColors.textPrimary);

      // Verify date range chip is rendered
      final dateFinder = find.text('February 2026 – June 2026');
      expect(dateFinder, findsOneWidget);
      final Text dateText = tester.widget(dateFinder);
      expect(dateText.style?.fontWeight, FontWeight.w600);
      expect(dateText.style?.color, AppColors.pastelLavenderText);

      // Verify description is rendered
      final descFinder = find.text(
          'Complete discovery and core vendor procurement for the event.');
      expect(descFinder, findsOneWidget);

      // Verify vertical ordering: Title top < Date Chip top < Description top
      final titleTop = tester.getTopLeft(titleFinder).dy;
      final dateTop = tester.getTopLeft(dateFinder).dy;
      final descTop = tester.getTopLeft(descFinder).dy;

      expect(titleTop, lessThan(dateTop));
      expect(dateTop, lessThan(descTop));
    });

    testWidgets(
        'renders gracefully on small phones (320px width) without overflow',
        (tester) async {
      tester.view.physicalSize = const Size(320 * 2, 600 * 2);
      tester.view.devicePixelRatio = 2.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      const item = TimelinePhaseItem(
        title:
            'Discovery and Comprehensive Vendor Procurement Across Multiple Categories',
        dateRange: 'February 2026 – June 2026',
        description:
            'Coordinate with catering, floral designers, sound technicians, and lighting professionals to secure early bird rates.',
      );

      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: SingleChildScrollView(
              child: Padding(
                padding: EdgeInsets.all(16),
                child: TimelinePhaseCard(item: item),
              ),
            ),
          ),
        ),
      );

      // Ensure no RenderFlex errors were thrown
      expect(tester.takeException(), isNull);
      expect(find.text('February 2026 – June 2026'), findsOneWidget);
    });

    testWidgets(
        'renders gracefully on tablets (768px width) without overflow',
        (tester) async {
      tester.view.physicalSize = const Size(768 * 2, 1024 * 2);
      tester.view.devicePixelRatio = 2.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      const item = TimelinePhaseItem(
        title: 'Initial Concept & Strategic Ideation',
        dateRange: 'Jan 1, 2027 – Jan 27, 2027',
        description: 'Brainstorm creative concepts and layout mockups.',
      );

      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Padding(
              padding: EdgeInsets.all(24),
              child: TimelinePhaseCard(item: item),
            ),
          ),
        ),
      );

      expect(tester.takeException(), isNull);
      expect(find.text('Jan 1, 2027 – Jan 27, 2027'), findsOneWidget);
      expect(find.text('Initial Concept & Strategic Ideation'), findsOneWidget);
    });
  });

  group('ProposedTimelineSection Widget', () {
    testWidgets('renders empty message when timeline map is empty',
        (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ProposedTimelineSection(
              title: 'Proposed Timeline',
              timeline: {},
            ),
          ),
        ),
      );

      expect(find.text('Proposed Timeline'), findsOneWidget);
      expect(find.text('No timeline steps provided.'), findsOneWidget);
    });

    testWidgets('renders with "Event Milestones" and "Schedule Breakdown" titles',
        (tester) async {
      final sampleTimeline = {
        'Discovery and Core Vendor Procurement':
            'February 2026 – June 2026: Complete discovery and core vendor procurement for the event.',
        'Production Setup': 'Jan 1, 2027 – Jan 27, 2027: Final setup.',
      };

      // Test Event Milestones title
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: SingleChildScrollView(
              child: ProposedTimelineSection(
                title: 'Event Milestones',
                timeline: sampleTimeline,
              ),
            ),
          ),
        ),
      );

      expect(find.text('Event Milestones'), findsOneWidget);
      expect(find.text('Discovery and Core Vendor Procurement'), findsOneWidget);
      expect(find.text('February 2026 – June 2026'), findsOneWidget);
      expect(find.text('Jan 1, 2027 – Jan 27, 2027'), findsOneWidget);

      // Test Schedule Breakdown title
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: SingleChildScrollView(
              child: ProposedTimelineSection(
                title: 'Schedule Breakdown',
                timeline: sampleTimeline,
              ),
            ),
          ),
        ),
      );

      expect(find.text('Schedule Breakdown'), findsOneWidget);
    });
  });
}
