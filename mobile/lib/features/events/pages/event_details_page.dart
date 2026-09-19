import 'package:flutter/material.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/event_remote_datasource.dart';
import '../models/event_model.dart';

class EventDetailsPage extends StatefulWidget {
  const EventDetailsPage({super.key});

  @override
  State<EventDetailsPage> createState() => _EventDetailsPageState();
}

class _EventDetailsPageState extends State<EventDetailsPage> {
  final _api = EventRemoteDataSource();
  EventModel? _event;
  AuthResponseModel? _auth;
  String? _error;
  bool _loading = true;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_auth == null) {
      final args = ModalRoute.of(context)?.settings.arguments as Map?;
      _auth = args?['auth'] as AuthResponseModel?;
      _event = args?['event'] as EventModel?;
      _load();
    }
  }

  Future<void> _load() async {
    if (_auth == null || _event == null) {
      setState(() {
        _loading = false;
        _error = 'Event not found.';
      });
      return;
    }
    try {
      final result = await _api.get(_auth!.accessToken, _event!.id);
      if (mounted) {
        setState(() {
          _event = result;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString();
          _loading = false;
        });
      }
    }
  }

  Future<void> _delete() async {
    final confirmed = await showDialog<bool>(
          context: context,
          builder: (_) => AlertDialog(
            title: const Text('Delete event?'),
            content: const Text('This cannot be undone.'),
            actions: [
              TextButton(
                  onPressed: () => Navigator.pop(context, false),
                  child: const Text('Cancel')),
              FilledButton(
                  onPressed: () => Navigator.pop(context, true),
                  child: const Text('Delete')),
            ],
          ),
        ) ??
        false;
    if (!confirmed || _auth == null || _event == null) return;
    try {
      await _api.delete(_auth!.accessToken, _event!.id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text('Event deleted.'), backgroundColor: Colors.green),
      );
      Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString()), backgroundColor: Colors.red),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final event = _event;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Event details'),
        actions: [
          if (event != null)
            IconButton(
              onPressed: () => Navigator.pushNamed(
                context,
                '/events/edit',
                arguments: {'auth': _auth, 'event': event},
              ),
              icon: const Icon(Icons.edit),
            ),
          if (event != null)
            IconButton(onPressed: _delete, icon: const Icon(Icons.delete)),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(_error!),
                      TextButton(onPressed: _load, child: const Text('Retry')),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView(
                    padding: const EdgeInsets.all(20),
                    children: [
                      Text(event!.preferredVenue,
                          style: Theme.of(context).textTheme.headlineSmall),
                      const SizedBox(height: 16),
                      _row('Type', event.eventType.name),
                      _row('Status', event.status.name),
                      _row('Guests', '${event.guestCount}'),
                      _row('Budget', event.budget.toStringAsFixed(2)),
                      _row('Preferred date',
                          event.preferredDate.toLocal().toString()),
                      _row('Requirements', event.requirements ?? 'None'),
                      _row('Created', event.createdAt.toLocal().toString()),
                    ],
                  ),
                ),
    );
  }

  Widget _row(String label, String value) => Card(
        child: ListTile(title: Text(label), subtitle: Text(value)),
      );
}
