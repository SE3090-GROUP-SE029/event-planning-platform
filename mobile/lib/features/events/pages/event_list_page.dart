import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_bottom_nav_bar.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_empty_state.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../../auth/models/auth_response_model.dart';
import '../models/event_model.dart';
import '../providers/event_providers.dart';

class EventListPage extends ConsumerStatefulWidget {
  const EventListPage({super.key});

  @override
  ConsumerState<EventListPage> createState() => _EventListPageState();
}

class _EventListPageState extends ConsumerState<EventListPage> {
  AuthResponseModel? _auth;
  final _scroll = ScrollController();

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _auth ??= ModalRoute.of(context)?.settings.arguments as AuthResponseModel?;
    if (_auth != null) {
      WidgetsBinding.instance.addPostFrameCallback(
          (_) => ref.read(eventListProvider(_auth!.accessToken)).load());
    }
  }

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.position.pixels > _scroll.position.maxScrollExtent - 300 &&
          _auth != null) {
        ref.read(eventListProvider(_auth!.accessToken)).load();
      }
    });
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_auth == null) {
      return const Scaffold(
        backgroundColor: AppColors.canvas,
        body: Center(
          child: Text('Please log in first.'),
        ),
      );
    }
    final controller = ref.watch(eventListProvider(_auth!.accessToken));

    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('My Events'),
        actions: [
          PopupMenuButton<String>(
            tooltip: 'Filter events',
            icon: Container(
              width: 40,
              height: 40,
              decoration: BoxDecoration(
                color: AppColors.surfacePure,
                borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
                border: Border.all(color: AppColors.borderSubtle),
              ),
              child: const Icon(
                Icons.filter_list_rounded,
                size: 20,
                color: AppColors.textPrimary,
              ),
            ),
            onSelected: (value) {
              if (value == 'clear') {
                controller.typeFilter = null;
                controller.statusFilter = null;
              } else if (value.startsWith('type:')) {
                controller.typeFilter = EventType.values
                    .firstWhere((item) => item.name == value.substring(5));
              } else if (value.startsWith('status:')) {
                controller.statusFilter = EventStatus.values
                    .firstWhere((item) => item.name == value.substring(7));
              }
              controller.load(refresh: true);
            },
            itemBuilder: (_) => [
              const PopupMenuItem(value: 'clear', child: Text('Clear filters')),
              const PopupMenuDivider(),
              ...EventType.values.map((type) => PopupMenuItem(
                    value: 'type:${type.name}',
                    child: Text('Type: ${type.name}'),
                  )),
              const PopupMenuDivider(),
              ...EventStatus.values.map((status) => PopupMenuItem(
                    value: 'status:${status.name}',
                    child: Text('Status: ${status.name}'),
                  )),
            ],
          ),
          const SizedBox(width: 8),
          PopupMenuButton<String>(
            tooltip: 'Sort events',
            initialValue: controller.sortBy,
            icon: Container(
              width: 40,
              height: 40,
              decoration: BoxDecoration(
                color: AppColors.surfacePure,
                borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
                border: Border.all(color: AppColors.borderSubtle),
              ),
              child: const Icon(
                Icons.sort_rounded,
                size: 20,
                color: AppColors.textPrimary,
              ),
            ),
            onSelected: (v) {
              controller.sortBy = v;
              controller.load(refresh: true);
            },
            itemBuilder: (_) => const [
              PopupMenuItem(value: 'createdAt', child: Text('Created date')),
              PopupMenuItem(value: 'preferredDate', child: Text('Event date')),
              PopupMenuItem(value: 'budget', child: Text('Budget')),
            ],
          ),
          const SizedBox(width: AppDimens.space16),
        ],
      ),
      bottomNavigationBar: PastelBottomNavBar(
        currentIndex: 1,
        onTap: (index) {
          if (index == 0) {
            Navigator.of(context).pop();
          } else if (index == 2 && _auth!.roles.contains('VENDOR')) {
            Navigator.of(context)
                .pushNamed('/vendors/profile', arguments: _auth);
          }
        },
        onCenterActionTap: () =>
            Navigator.pushNamed(context, '/events/create', arguments: _auth),
      ),
      body: RefreshIndicator(
        color: AppColors.obsidianBlack,
        backgroundColor: AppColors.surfacePure,
        onRefresh: () => controller.load(refresh: true),
        child: controller.loading && controller.events.isEmpty
            ? const Center(
                child: CircularProgressIndicator(
                  valueColor:
                      AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
                ),
              )
            : controller.hasError && controller.events.isEmpty
                ? ListView(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    children: [
                      const SizedBox(height: 120),
                      PastelEmptyState(
                        icon: Icons.error_outline_rounded,
                        iconVariant: PastelIconVariant.pink,
                        title: 'Unable to load events',
                        description:
                            'Something went wrong while fetching your events. Please try again.',
                        actionLabel: 'Retry',
                        onActionTap: () => controller.load(refresh: true),
                      ),
                    ],
                  )
                : controller.events.isEmpty
                    ? ListView(
                        padding: const EdgeInsets.all(AppDimens.space20),
                        children: [
                          const SizedBox(height: 100),
                          PastelEmptyState(
                            icon: Icons.calendar_today_outlined,
                            iconVariant: PastelIconVariant.yellow,
                            title: 'No events yet',
                            description:
                                'Create your first event plan to start organizing vendors, guests, and budgets.',
                            actionLabel: 'Create event',
                            onActionTap: () => Navigator.pushNamed(
                              context,
                              '/events/create',
                              arguments: _auth,
                            ),
                          ),
                        ],
                      )
                    : ListView.builder(
                        controller: _scroll,
                        padding: const EdgeInsets.fromLTRB(
                          AppDimens.space18,
                          AppDimens.space12,
                          AppDimens.space18,
                          AppDimens.space24,
                        ),
                        itemCount: controller.events.length +
                            (controller.loadingMore ? 1 : 0),
                        itemBuilder: (_, index) {
                          if (index == controller.events.length) {
                            return const Center(
                              child: Padding(
                                padding: EdgeInsets.all(AppDimens.space16),
                                child: CircularProgressIndicator(
                                  valueColor: AlwaysStoppedAnimation<Color>(
                                      AppColors.obsidianBlack),
                                ),
                              ),
                            );
                          }

                          final event = controller.events[index];
                          final dateText = event.preferredDate
                              .toLocal()
                              .toString()
                              .split(' ')
                              .first;

                          return Padding(
                            padding: const EdgeInsets.only(
                                bottom: AppDimens.space14),
                            child: PastelCard(
                              padding: const EdgeInsets.all(AppDimens.space18),
                              onTap: () async {
                                await Navigator.pushNamed(
                                  context,
                                  '/events/details',
                                  arguments: {'auth': _auth, 'event': event},
                                );
                                controller.load(refresh: true);
                              },
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  // Header row: Type badge & Status badge
                                  Row(
                                    children: [
                                      PastelPillBadge(
                                        text: event.eventType.name.toUpperCase(),
                                        style: _getTypeBadgeStyle(
                                            event.eventType),
                                      ),
                                      const Spacer(),
                                      PastelPillBadge(
                                        text: event.status.name.toUpperCase(),
                                        style: _getStatusBadgeStyle(
                                            event.status),
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: AppDimens.space14),

                                  // Venue / Title
                                  Text(
                                    event.preferredVenue.isEmpty
                                        ? 'Untitled Event'
                                        : event.preferredVenue,
                                    style: const TextStyle(
                                      color: AppColors.textPrimary,
                                      fontSize: 18,
                                      fontWeight: FontWeight.w800,
                                      letterSpacing: -0.3,
                                    ),
                                  ),
                                  const SizedBox(height: AppDimens.space12),

                                  // Information Badges row
                                  Wrap(
                                    spacing: 8,
                                    runSpacing: 8,
                                    children: [
                                      _buildInfoBadge(
                                        Icons.calendar_today_outlined,
                                        dateText,
                                      ),
                                      _buildInfoBadge(
                                        Icons.people_outline_rounded,
                                        '${event.guestCount} guests',
                                      ),
                                      _buildInfoBadge(
                                        Icons.attach_money_rounded,
                                        event.budget.toStringAsFixed(0),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          );
                        },
                      ),
      ),
    );
  }

  Widget _buildInfoBadge(IconData icon, String text) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: AppColors.surfaceMuted,
        borderRadius: BorderRadius.circular(AppDimens.radiusSmall),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: AppColors.textSecondary),
          const SizedBox(width: 5),
          Text(
            text,
            style: const TextStyle(
              color: AppColors.textPrimary,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }

  PastelBadgeStyle _getTypeBadgeStyle(EventType type) {
    switch (type) {
      case EventType.wedding:
        return PastelBadgeStyle.pink;
      case EventType.birthday:
        return PastelBadgeStyle.yellow;
      case EventType.corporate:
        return PastelBadgeStyle.blue;
      case EventType.anniversary:
        return PastelBadgeStyle.lavender;
      case EventType.other:
        return PastelBadgeStyle.neutral;
    }
  }

  PastelBadgeStyle _getStatusBadgeStyle(EventStatus status) {
    switch (status) {
      case EventStatus.confirmed:
        return PastelBadgeStyle.green;
      case EventStatus.draft:
        return PastelBadgeStyle.yellow;
      case EventStatus.planning:
      case EventStatus.inPlanning:
        return PastelBadgeStyle.blue;
      case EventStatus.completed:
        return PastelBadgeStyle.oliveRibbon;
      case EventStatus.cancelled:
        return PastelBadgeStyle.neutral;
    }
  }
}
