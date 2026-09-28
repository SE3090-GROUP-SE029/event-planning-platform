import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';

/// Parsed data for a single phase in an AI-generated event timeline.
class TimelinePhaseItem {
  final String title;
  final String? dateRange;
  final String? description;

  const TimelinePhaseItem({
    required this.title,
    this.dateRange,
    this.description,
  });

  /// Factory that parses a raw [key] and [value] from the timeline dictionary.
  /// Handles various AI generation formats:
  /// - Agentic AI format: key = "Discovery and Core Vendor Procurement", value = "February 2026 – June 2026: Complete discovery and core vendor procurement for the event."
  /// - Newline delimited: key = "Phase Title", value = "February 2026 – June 2026\nDescription..."
  /// - Key as date range: key = "February 2026 – June 2026", value = "Discovery and Core Vendor Procurement"
  /// - Value as timing only: key = "Setup", value = "2 hours before event"
  /// - Value as description only: key = "Phase Title", value = "Description of the phase"
  factory TimelinePhaseItem.fromEntry(String key, String value) {
    final trimmedKey = key.trim();
    final trimmedVal = value.trim();

    // 1. Check for newline separation (e.g. "Timing\nDescription")
    if (trimmedVal.contains('\n')) {
      final lines = trimmedVal
          .split('\n')
          .map((l) => l.trim())
          .where((l) => l.isNotEmpty)
          .toList();
      if (lines.length >= 2) {
        return TimelinePhaseItem(
          title: trimmedKey,
          dateRange: lines.first,
          description: lines.sublist(1).join('\n'),
        );
      }
    }

    // 2. Check for colon followed by space ": " separating timing from description
    // Avoids clock times like "10:00 AM" which do not have a space after the first colon.
    final colonSpaceMatch = RegExp(r':\s+').firstMatch(trimmedVal);
    if (colonSpaceMatch != null && colonSpaceMatch.start > 0) {
      final timingCandidate = trimmedVal.substring(0, colonSpaceMatch.start).trim();
      final descCandidate = trimmedVal.substring(colonSpaceMatch.end).trim();

      // Only treat as date/timing if the candidate is not exceedingly long (e.g. <= 80 chars)
      if (timingCandidate.isNotEmpty &&
          timingCandidate.length <= 80 &&
          descCandidate.isNotEmpty) {
        return TimelinePhaseItem(
          title: trimmedKey,
          dateRange: timingCandidate,
          description: descCandidate,
        );
      }
    }

    // 3. Check if key is a date/time and value is the title or description
    final isKeyDateLike = _isDateOrTimingLike(trimmedKey);
    final isValDateLike = _isDateOrTimingLike(trimmedVal);

    if (isKeyDateLike && !isValDateLike) {
      return TimelinePhaseItem(
        title: trimmedVal,
        dateRange: trimmedKey,
        description: null,
      );
    }

    // 4. Value is date/timing only (no separate description)
    if (isValDateLike) {
      return TimelinePhaseItem(
        title: trimmedKey,
        dateRange: trimmedVal,
        description: null,
      );
    }

    // 5. Short string without sentence punctuation is likely a timing phrase
    if (trimmedVal.length <= 40 &&
        !trimmedVal.endsWith('.') &&
        trimmedVal.isNotEmpty) {
      return TimelinePhaseItem(
        title: trimmedKey,
        dateRange: trimmedVal,
        description: null,
      );
    }

    // 6. Otherwise value is a description with no timing specified
    return TimelinePhaseItem(
      title: trimmedKey,
      dateRange: null,
      description: trimmedVal.isNotEmpty ? trimmedVal : null,
    );
  }

  static bool _isDateOrTimingLike(String text) {
    if (text.isEmpty) return false;
    final lower = text.toLowerCase();
    const dateKeywords = [
      'jan', 'feb', 'mar', 'apr', 'may', 'jun',
      'jul', 'aug', 'sep', 'oct', 'nov', 'dec',
      'week', 'month', 'day', 'hour', 'minute',
      'before', 'after', 'prior', 'during', 'setup',
      'q1', 'q2', 'q3', 'q4', '2025', '2026', '2027', '2028', '2029', '2030'
    ];
    final hasKeyword = dateKeywords.any((kw) => lower.contains(kw));
    final hasRangeSymbol =
        text.contains('–') || text.contains('—') || text.contains('-') || lower.contains(' to ');
    return hasKeyword || hasRangeSymbol;
  }
}

/// A responsive, beautifully styled card displaying a single timeline phase.
/// Stacks elements vertically:
/// 1. Phase Title (high contrast, bold, large font)
/// 2. Date Range Chip / Tag (AppColors.pastelLavender background, fully readable)
/// 3. Description (normal body text, comfortable line height)
class TimelinePhaseCard extends StatelessWidget {
  final TimelinePhaseItem item;
  final Color backgroundColor;
  final Color chipColor;
  final Color chipTextColor;

  const TimelinePhaseCard({
    super.key,
    required this.item,
    this.backgroundColor = AppColors.surfacePure,
    this.chipColor = AppColors.pastelLavender,
    this.chipTextColor = AppColors.pastelLavenderText,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(AppDimens.space16),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
        border: Border.all(
          color: chipColor.withValues(alpha: 0.8),
          width: 1.2,
        ),
        boxShadow: [
          BoxShadow(
            color: chipTextColor.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          // 1. Phase Title
          Text(
            item.title,
            style: const TextStyle(
              color: AppColors.textPrimary,
              fontSize: 15.5,
              fontWeight: FontWeight.w700,
              height: 1.3,
              letterSpacing: -0.2,
            ),
          ),

          // 2. Date Range Chip / Tag
          if (item.dateRange != null && item.dateRange!.trim().isNotEmpty) ...[
            const SizedBox(height: AppDimens.space12),
            Align(
              alignment: Alignment.centerLeft,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                decoration: BoxDecoration(
                  color: chipColor,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(
                    color: chipTextColor.withValues(alpha: 0.15),
                    width: 0.8,
                  ),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      Icons.schedule_rounded,
                      size: 13,
                      color: chipTextColor,
                    ),
                    const SizedBox(width: 6),
                    Flexible(
                      child: Text(
                        item.dateRange!.trim(),
                        style: TextStyle(
                          color: chipTextColor,
                          fontSize: 12.5,
                          fontWeight: FontWeight.w600,
                          letterSpacing: -0.1,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],

          // 3. Description
          if (item.description != null && item.description!.trim().isNotEmpty) ...[
            const SizedBox(height: AppDimens.space10),
            Text(
              item.description!.trim(),
              style: const TextStyle(
                color: AppColors.textSecondary,
                fontSize: 13.5,
                fontWeight: FontWeight.w400,
                height: 1.5,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

/// A comprehensive section card for AI-generated timeline sections:
/// - "Proposed Timeline"
/// - "Event Milestones"
/// - "Schedule Breakdown"
///
/// Features a pastel lavender card container with an icon header and vertically
/// stacked, mobile-responsive timeline phase cards.
class ProposedTimelineSection extends StatelessWidget {
  final String title;
  final IconData icon;
  final Map<String, String> timeline;
  final String emptyMessage;

  const ProposedTimelineSection({
    super.key,
    this.title = 'Proposed Timeline',
    this.icon = Icons.schedule_rounded,
    required this.timeline,
    this.emptyMessage = 'No timeline steps provided.',
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(AppDimens.space20),
      decoration: BoxDecoration(
        color: AppColors.pastelLavenderLight,
        borderRadius: BorderRadius.circular(AppDimens.radiusCard),
        border: Border.all(
          color: const Color(0x30E5D4F7),
          width: 1.2,
        ),
        boxShadow: AppDimens.cardShadow,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          // Section Header
          Row(
            children: [
              Icon(icon, color: AppColors.pastelLavenderText, size: 20),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  title,
                  style: const TextStyle(
                    color: AppColors.pastelLavenderText,
                    fontSize: 16,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.3,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: AppDimens.space14),

          // Section Content
          if (timeline.isEmpty)
            Text(
              emptyMessage,
              style: const TextStyle(
                color: AppColors.textSecondary,
                fontSize: 13,
              ),
            )
          else
            Column(
              mainAxisSize: MainAxisSize.min,
              children: timeline.entries.map((entry) {
                final item = TimelinePhaseItem.fromEntry(entry.key, entry.value);
                return Padding(
                  padding: const EdgeInsets.only(bottom: AppDimens.space12),
                  child: TimelinePhaseCard(item: item),
                );
              }).toList(),
            ),
        ],
      ),
    );
  }
}
