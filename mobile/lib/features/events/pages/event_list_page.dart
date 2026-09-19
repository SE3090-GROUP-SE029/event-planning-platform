import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
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
      return const Scaffold(body: Center(child: Text('Please log in first.')));
    }
    final controller = ref.watch(eventListProvider(_auth!.accessToken));
    return Scaffold(
      appBar: AppBar(title: const Text('My Events'), actions: [
        PopupMenuButton<String>(
            icon: const Icon(Icons.filter_list),
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
                  const PopupMenuItem(
                      value: 'clear', child: Text('Clear filters')),
                  const PopupMenuDivider(),
                  ...EventType.values.map((type) => PopupMenuItem(
                      value: 'type:${type.name}',
                      child: Text('Type: ${type.name}'))),
                  const PopupMenuDivider(),
                  ...EventStatus.values.map((status) => PopupMenuItem(
                      value: 'status:${status.name}',
                      child: Text('Status: ${status.name}'))),
                ]),
        PopupMenuButton<String>(
            initialValue: controller.sortBy,
            onSelected: (v) {
              controller.sortBy = v;
              controller.load(refresh: true);
            },
            itemBuilder: (_) => const [
                  PopupMenuItem(
                      value: 'createdAt', child: Text('Created date')),
                  PopupMenuItem(
                      value: 'preferredDate', child: Text('Event date')),
                  PopupMenuItem(value: 'budget', child: Text('Budget'))
                ]),
      ]),
      floatingActionButton: FloatingActionButton(
          onPressed: () =>
              Navigator.pushNamed(context, '/events/create', arguments: _auth),
          child: const Icon(Icons.add)),
      body: RefreshIndicator(
          onRefresh: () => controller.load(refresh: true),
          child: controller.loading && controller.events.isEmpty
              ? const Center(child: CircularProgressIndicator())
              : controller.hasError && controller.events.isEmpty
                  ? ListView(children: [
                      const SizedBox(height: 180),
                      const Center(child: Text('Unable to load events')),
                      Center(
                          child: TextButton(
                              onPressed: () => controller.load(refresh: true),
                              child: const Text('Retry')))
                    ])
                  : controller.events.isEmpty
                      ? ListView(children: const [
                          SizedBox(height: 180),
                          Center(
                              child: Text(
                                  'No events yet. Create your first event.'))
                        ])
                      : ListView.builder(
                          controller: _scroll,
                          itemCount: controller.events.length +
                              (controller.loadingMore ? 1 : 0),
                          itemBuilder: (_, index) {
                            if (index == controller.events.length) {
                              return const Center(
                                  child: Padding(
                                      padding: EdgeInsets.all(16),
                                      child: CircularProgressIndicator()));
                            }
                            final event = controller.events[index];
                            return ListTile(
                                title: Text(event.preferredVenue.isEmpty
                                    ? 'Event'
                                    : event.preferredVenue),
                                subtitle: Text(
                                    '${event.eventType.name} • ${event.status.name} • ${event.preferredDate.toLocal().toString().split(' ').first}'),
                                trailing: const Icon(Icons.chevron_right),
                                onTap: () async {
                                  await Navigator.pushNamed(
                                      context, '/events/details', arguments: {
                                    'auth': _auth,
                                    'event': event
                                  });
                                  controller.load(refresh: true);
                                });
                          })),
    );
  }
}
