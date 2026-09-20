import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_section_header.dart';
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
  final _guests = TextEditingController();
  final _budget = TextEditingController();
  final _venue = TextEditingController();
  final _requirements = TextEditingController();
  final _api = EventRemoteDataSource();

  EventType _type = EventType.wedding;
  EventStatus _status = EventStatus.draft;
  DateTime _date = DateTime.now().add(const Duration(days: 1));
  bool _loading = true;
  bool _saving = false;
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
      lastDate: DateTime.now().add(const Duration(days: 3650)),
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: const ColorScheme.light(
              primary: AppColors.obsidianBlack,
              onPrimary: Colors.white,
              surface: AppColors.surfacePure,
              onSurface: AppColors.textPrimary,
            ),
          ),
          child: child!,
        );
      },
    );
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
      requirements:
          _requirements.text.trim().isEmpty ? null : _requirements.text.trim(),
      status: _status,
      createdAt: _event?.createdAt ?? DateTime.now(),
      updatedAt: _event?.updatedAt,
    );
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      final saved = widget.isEditing
          ? await _api.update(_auth!.accessToken, event.id, event)
          : await _api.create(_auth!.accessToken, event);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(widget.isEditing
              ? 'Event updated successfully.'
              : 'Event created successfully.'),
          backgroundColor: AppColors.success,
        ),
      );
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
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: Text(widget.isEditing ? 'Edit event' : 'Create event'),
      ),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(
                valueColor:
                    AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
              ),
            )
          : Form(
              key: _formKey,
              child: ListView(
                padding: const EdgeInsets.fromLTRB(
                  AppDimens.space20,
                  AppDimens.space12,
                  AppDimens.space20,
                  AppDimens.space32,
                ),
                children: [
                  if (_error != null)
                    Container(
                      padding: const EdgeInsets.all(AppDimens.space14),
                      margin: const EdgeInsets.only(bottom: AppDimens.space16),
                      decoration: BoxDecoration(
                        color: AppColors.errorBg,
                        borderRadius:
                            BorderRadius.circular(AppDimens.radiusMedium),
                        border: Border.all(color: const Color(0x30C7434D)),
                      ),
                      child: Row(
                        children: [
                          const Icon(Icons.error_outline_rounded,
                              color: AppColors.error, size: 20),
                          const SizedBox(width: 10),
                          Expanded(
                            child: Text(
                              _error!,
                              style: const TextStyle(
                                color: AppColors.error,
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),

                  // Section 1: Event Information
                  const PastelSectionHeader(
                    title: 'Event Information',
                    padding: EdgeInsets.only(bottom: AppDimens.space12),
                  ),
                  PastelCard(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Event Type',
                          style: Theme.of(context).textTheme.titleSmall?.copyWith(
                                fontWeight: FontWeight.w700,
                              ),
                        ),
                        const SizedBox(height: 8),
                        DropdownButtonFormField<EventType>(
                          initialValue: _type,
                          decoration: const InputDecoration(
                            hintText: 'Select type',
                          ),
                          items: EventType.values
                              .map((e) => DropdownMenuItem(
                                    value: e,
                                    child: Text(
                                      e.name.toUpperCase(),
                                      style: const TextStyle(
                                          fontWeight: FontWeight.w600),
                                    ),
                                  ))
                              .toList(),
                          onChanged: (v) => setState(() => _type = v!),
                        ),
                        const SizedBox(height: AppDimens.space16),
                        TextFormField(
                          controller: _venue,
                          decoration: const InputDecoration(
                            labelText: 'Preferred venue',
                            hintText: 'e.g. Grand Plaza Ballroom',
                            prefixIcon: Icon(
                              Icons.location_on_outlined,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          validator: (v) =>
                              v == null || v.trim().isEmpty ? 'Required' : null,
                        ),
                      ],
                    ),
                  ),

                  // Section 2: Logistics & Schedule
                  const PastelSectionHeader(title: 'Logistics & Schedule'),
                  PastelCard(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Column(
                      children: [
                        TextFormField(
                          controller: _guests,
                          decoration: const InputDecoration(
                            labelText: 'Guest count',
                            hintText: 'e.g. 150',
                            prefixIcon: Icon(
                              Icons.people_outline_rounded,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          keyboardType: TextInputType.number,
                          validator: (v) =>
                              int.tryParse(v ?? '') == null ||
                                      int.parse(v!) <= 0
                                  ? 'Enter a positive guest count'
                                  : null,
                        ),
                        const SizedBox(height: AppDimens.space16),
                        TextFormField(
                          controller: _budget,
                          decoration: const InputDecoration(
                            labelText: 'Budget (\$)',
                            hintText: 'e.g. 5000',
                            prefixIcon: Icon(
                              Icons.attach_money_rounded,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          keyboardType: const TextInputType.numberWithOptions(
                              decimal: true),
                          validator: (v) =>
                              double.tryParse(v ?? '') == null ||
                                      double.parse(v!) < 0
                                  ? 'Enter a valid budget'
                                  : null,
                        ),
                        const SizedBox(height: AppDimens.space16),

                        // Date picker row
                        InkWell(
                          onTap: _pickDate,
                          borderRadius:
                              BorderRadius.circular(AppDimens.radiusMedium),
                          child: Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: AppDimens.space16,
                              vertical: AppDimens.space14,
                            ),
                            decoration: BoxDecoration(
                              color: AppColors.surfaceMuted,
                              borderRadius:
                                  BorderRadius.circular(AppDimens.radiusMedium),
                              border: Border.all(color: AppColors.borderSubtle),
                            ),
                            child: Row(
                              children: [
                                const PastelIconBadge(
                                  icon: Icons.calendar_today_outlined,
                                  variant: PastelIconVariant.yellow,
                                  size: 38,
                                  iconSize: 18,
                                ),
                                const SizedBox(width: AppDimens.space14),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      const Text(
                                        'Event date',
                                        style: TextStyle(
                                          color: AppColors.textSecondary,
                                          fontSize: 12,
                                          fontWeight: FontWeight.w500,
                                        ),
                                      ),
                                      const SizedBox(height: 2),
                                      Text(
                                        _date
                                            .toLocal()
                                            .toString()
                                            .split(' ')
                                            .first,
                                        style: const TextStyle(
                                          color: AppColors.textPrimary,
                                          fontSize: 15,
                                          fontWeight: FontWeight.w700,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                                const Text(
                                  'Change',
                                  style: TextStyle(
                                    color: AppColors.obsidianBlack,
                                    fontSize: 13,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),

                  // Section 3: Status & Requirements
                  const PastelSectionHeader(title: 'Details & Requirements'),
                  PastelCard(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (widget.isEditing) ...[
                          Text(
                            'Status',
                            style:
                                Theme.of(context).textTheme.titleSmall?.copyWith(
                                      fontWeight: FontWeight.w700,
                                    ),
                          ),
                          const SizedBox(height: 8),
                          DropdownButtonFormField<EventStatus>(
                            initialValue: _status,
                            decoration: const InputDecoration(
                              hintText: 'Select status',
                            ),
                            items: EventStatus.values
                                .map((e) => DropdownMenuItem(
                                      value: e,
                                      child: Text(
                                        e.name.toUpperCase(),
                                        style: const TextStyle(
                                            fontWeight: FontWeight.w600),
                                      ),
                                    ))
                                .toList(),
                            onChanged: (v) => setState(() => _status = v!),
                          ),
                          const SizedBox(height: AppDimens.space16),
                        ],
                        TextFormField(
                          controller: _requirements,
                          decoration: const InputDecoration(
                            labelText: 'Special requirements',
                            hintText:
                                'e.g. Dietary restrictions, audio/video setup, parking needs...',
                            alignLabelWithHint: true,
                          ),
                          maxLines: 3,
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: AppDimens.space24),

                  // Save CTA Button
                  SizedBox(
                    width: double.infinity,
                    height: 52,
                    child: ElevatedButton(
                      onPressed: _saving ? null : _save,
                      child: _saving
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                valueColor:
                                    AlwaysStoppedAnimation<Color>(Colors.white),
                              ),
                            )
                          : Text(widget.isEditing
                              ? 'Save changes'
                              : 'Create event plan'),
                    ),
                  ),
                ],
              ),
            ),
    );
  }
}
