import 'package:flutter/material.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/event_remote_datasource.dart';
import '../models/event_model.dart';

class EventFormPage extends StatefulWidget {
  final bool isEditing;
  const EventFormPage({super.key, this.isEditing = false});
  @override
  State<EventFormPage> createState() => _EventFormPageState();
}

class _EventFormPageState extends State<EventFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _guests = TextEditingController(), _budget = TextEditingController();
  final _venue = TextEditingController(),
      _requirements = TextEditingController();
  final _api = EventRemoteDataSource();
  EventType _type = EventType.wedding;
  EventStatus _status = EventStatus.draft;
  DateTime _date = DateTime.now().add(const Duration(days: 1));
  bool _loading = true, _saving = false;
  String? _error;
  AuthResponseModel? _auth;
  EventModel? _event;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_auth == null) {
      final args = ModalRoute.of(context)?.settings.arguments;
      if (args is AuthResponseModel) _auth = args;
      if (args is Map) {
        _auth = args['auth'] as AuthResponseModel?;
        _event = args['event'] as EventModel?;
      }
      _event == null && widget.isEditing ? _loadEvent() : _populate();
    }
  }

  void _populate() {
    final event = _event;
    if (event != null) {
      _type = event.eventType;
      _status = event.status;
      _date = event.preferredDate;
      _guests.text = '${event.guestCount}';
      _budget.text = '${event.budget}';
      _venue.text = event.preferredVenue;
      _requirements.text = event.requirements ?? '';
    }
    setState(() => _loading = false);
  }

  Future<void> _loadEvent() async {
    final args = ModalRoute.of(context)?.settings.arguments as Map?;
    final id = args?['eventId'] as String?;
    if (_auth == null || id == null) {
      setState(() {
        _loading = false;
        _error = 'Event information is missing.';
      });
      return;
    }
    try {
      _event = await _api.get(_auth!.accessToken, id);
      _populate();
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = e.toString();
        });
      }
    }
  }

  @override
  void dispose() {
    _guests.dispose();
    _budget.dispose();
    _venue.dispose();
    _requirements.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final tomorrow = DateTime.now().add(const Duration(days: 1));
    final value = await showDatePicker(
        context: context,
        initialDate: _date.isAfter(tomorrow) ? _date : tomorrow,
        firstDate: tomorrow,
        lastDate: DateTime.now().add(const Duration(days: 3650)));
    if (value != null) setState(() => _date = value);
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false) || _auth == null) return;
    final event = EventModel(
        id: _event?.id ?? '',
        ownerId: _auth!.userId,
        eventType: _type,
        guestCount: int.parse(_guests.text),
        budget: double.parse(_budget.text),
        preferredVenue: _venue.text.trim(),
        preferredDate: _date,
        eventDuration: _event?.eventDuration ?? const Duration(hours: 1),
        requirements: _requirements.text.trim().isEmpty
            ? null
            : _requirements.text.trim(),
        status: _status,
        createdAt: _event?.createdAt ?? DateTime.now(),
        updatedAt: _event?.updatedAt);
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      final saved = widget.isEditing
          ? await _api.update(_auth!.accessToken, event.id, event)
          : await _api.create(_auth!.accessToken, event);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(widget.isEditing ? 'Event updated.' : 'Event created.'),
          backgroundColor: Colors.green));
      Navigator.pop(context, saved);
    } catch (e) {
      if (mounted) {
        setState(() {
          _saving = false;
          _error = e.toString();
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(
            title: Text(widget.isEditing ? 'Edit Event' : 'Create Event')),
        body: _loading
            ? const Center(child: CircularProgressIndicator())
            : Form(
                key: _formKey,
                child: ListView(padding: const EdgeInsets.all(20), children: [
                  if (_error != null)
                    Text(_error!, style: const TextStyle(color: Colors.red)),
                  DropdownButtonFormField<EventType>(
                      initialValue: _type,
                      decoration:
                          const InputDecoration(labelText: 'Event type'),
                      items: EventType.values
                          .map((e) =>
                              DropdownMenuItem(value: e, child: Text(e.name)))
                          .toList(),
                      onChanged: (v) => setState(() => _type = v!)),
                  TextFormField(
                      controller: _guests,
                      decoration:
                          const InputDecoration(labelText: 'Guest count'),
                      keyboardType: TextInputType.number,
                      validator: (v) =>
                          int.tryParse(v ?? '') == null || int.parse(v!) <= 0
                              ? 'Enter a positive guest count'
                              : null),
                  TextFormField(
                      controller: _budget,
                      decoration: const InputDecoration(labelText: 'Budget'),
                      keyboardType:
                          const TextInputType.numberWithOptions(decimal: true),
                      validator: (v) => double.tryParse(v ?? '') == null ||
                              double.parse(v!) < 0
                          ? 'Enter a valid budget'
                          : null),
                  TextFormField(
                      controller: _venue,
                      decoration:
                          const InputDecoration(labelText: 'Preferred venue'),
                      validator: (v) =>
                          v == null || v.trim().isEmpty ? 'Required' : null),
                  ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text(
                          'Preferred date: ${_date.toLocal().toString().split(' ').first}'),
                      trailing: const Icon(Icons.calendar_today),
                      onTap: _pickDate),
                  if (widget.isEditing)
                    DropdownButtonFormField<EventStatus>(
                        initialValue: _status,
                        decoration: const InputDecoration(labelText: 'Status'),
                        items: EventStatus.values
                            .map((e) =>
                                DropdownMenuItem(value: e, child: Text(e.name)))
                            .toList(),
                        onChanged: (v) => setState(() => _status = v!)),
                  TextFormField(
                      controller: _requirements,
                      decoration:
                          const InputDecoration(labelText: 'Requirements'),
                      maxLines: 3),
                  const SizedBox(height: 24),
                  ElevatedButton(
                      onPressed: _saving ? null : _save,
                      child: _saving
                          ? const CircularProgressIndicator()
                          : Text(widget.isEditing ? 'Save' : 'Create')),
                ]),
              ),
      );
}
