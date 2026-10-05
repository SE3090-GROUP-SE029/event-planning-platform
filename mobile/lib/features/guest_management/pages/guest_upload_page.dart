import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:file_picker/file_picker.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../core/theme/app_theme.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../../auth/models/auth_response_model.dart';
import '../../events/models/event_model.dart';
import '../api/guest_management_remote_datasource.dart';
import '../models/guest_registration_model.dart';

/// Planner-only screen for uploading a guest list (CSV, PDF, DOCX).
/// Displays structured results including duplicate/error rows.
class GuestUploadPage extends StatefulWidget {
  final AuthResponseModel auth;
  final EventModel event;

  const GuestUploadPage({
    super.key,
    required this.auth,
    required this.event,
  });

  @override
  State<GuestUploadPage> createState() => _GuestUploadPageState();
}

enum _UploadState { idle, uploading, success, error }

class _GuestUploadPageState extends State<GuestUploadPage> {
  late final GuestManagementRemoteDataSource _api;

  // File state
  String? _selectedFileName;
  int? _selectedFileSize;
  List<int>? _selectedFileBytes;

  // Upload state
  _UploadState _state = _UploadState.idle;
  double? _uploadProgress;
  BulkUploadResult? _result;
  String? _error;

  @override
  void initState() {
    super.initState();
    _api = GuestManagementRemoteDataSource();
  }

  Future<void> _pickFile() async {
    showModalBottomSheet<void>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppDimens.radiusSheet),
        ),
      ),
      builder: (_) => _FileTypeSheet(
        onSelectFile: () async {
          Navigator.pop(context);
          try {
            final selection = await FilePicker.pickFiles(
              type: FileType.custom,
              allowedExtensions: const ['csv', 'pdf', 'docx'],
              allowMultiple: false,
              withData: true,
            );
            if (selection == null || !mounted) return;

            final file = selection.files.single;
            if (!_isValidExtension(file.name)) {
              _showSelectionError(
                'Only CSV, PDF, and DOCX files are supported.',
              );
              return;
            }
            if (file.size > 5 * 1024 * 1024) {
              _showSelectionError('File must not exceed 5 MB.');
              return;
            }
            final bytes = file.bytes;
            if (bytes == null || bytes.isEmpty) {
              _showSelectionError(
                'The selected file could not be read. Please try again.',
              );
              return;
            }
            setState(() {
              _selectedFileName = file.name;
              _selectedFileSize = bytes.length;
              _selectedFileBytes = bytes;
              _state = _UploadState.idle;
              _result = null;
              _error = null;
            });
          } catch (e) {
            if (!mounted) return;
            _showSelectionError('Unable to select file: $e');
          }
        },
      ),
    );
  }

  bool _isValidExtension(String name) {
    final ext = name.split('.').last.toLowerCase();
    return ['csv', 'pdf', 'docx'].contains(ext);
  }

  void _showSelectionError(String message) {
    setState(() {
      _selectedFileName = null;
      _selectedFileSize = null;
      _selectedFileBytes = null;
      _error = message;
      _state = _UploadState.error;
    });
  }

  Future<void> _upload() async {
    final bytes = _selectedFileBytes;
    final name = _selectedFileName;
    if (bytes == null || name == null) return;

    if (bytes.length > 5 * 1024 * 1024) {
      setState(() {
        _error = 'File must not exceed 5 MB.';
        _state = _UploadState.error;
      });
      return;
    }

    setState(() {
      _state = _UploadState.uploading;
      _uploadProgress = 0;
      _error = null;
      _result = null;
    });

    try {
      final result = await _api.uploadGuestList(
        widget.event.id,
        bytes,
        name,
        onSendProgress: (sent, total) {
          if (!mounted || total <= 0) return;
          setState(() => _uploadProgress = sent / total);
        },
      );
      if (mounted) {
        setState(() {
          _result = result;
          _state = _UploadState.success;
          _uploadProgress = null;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = _parseError(e);
          _state = _UploadState.error;
          _uploadProgress = null;
        });
      }
    }
  }

  void _reset() {
    setState(() {
      _state = _UploadState.idle;
      _uploadProgress = null;
      _result = null;
      _error = null;
      _selectedFileName = null;
      _selectedFileSize = null;
      _selectedFileBytes = null;
    });
  }

  String _parseError(Object e) {
    if (e is ArgumentError) {
      return e.message?.toString() ?? 'The selected file is not supported.';
    }
    if (e is DioException) {
      final serverMessage = _serverMessage(e.response?.data);
      if (serverMessage != null) return serverMessage;
      if (e.response?.statusCode == 401) {
        return 'Your session has expired. Please sign in again.';
      }
      if (e.response?.statusCode == 403) {
        return 'You do not have permission to upload this guest list.';
      }
      if (e.response?.statusCode == 413) {
        return 'File must not exceed 5 MB.';
      }
    }
    final msg = e.toString();
    final serverMessage = _serverMessage(msg);
    if (serverMessage != null) return serverMessage;
    if (msg.contains('invalid_file_type')) {
      return 'Only CSV, PDF, and DOCX files are supported.';
    }
    if (msg.contains('file_too_large')) return 'File must not exceed 5 MB.';
    if (msg.contains('file_required')) return 'Please select a file first.';
    return 'Upload failed. Please try again.';
  }

  String? _serverMessage(Object? payload) {
    Object? decoded = payload;
    if (payload is String) {
      try {
        decoded = jsonDecode(payload);
      } on FormatException {
        final text = payload.trim();
        return text.isEmpty ? null : text;
      }
    }

    if (decoded is Map) {
      for (final key in const ['message', 'error', 'detail', 'title']) {
        final value = decoded[key];
        if (value is String && value.trim().isNotEmpty) return value;
      }
      final details = decoded['details'];
      if (details is List) {
        for (final detail in details) {
          if (detail is Map && detail['message'] is String) {
            final message = (detail['message'] as String).trim();
            if (message.isNotEmpty) return message;
          }
          if (detail is String && detail.trim().isNotEmpty) return detail;
        }
      }
      final errors = decoded['errors'];
      if (errors is Map) {
        for (final messages in errors.values) {
          if (messages is List && messages.isNotEmpty) {
            return messages.first.toString();
          }
          if (messages is String && messages.trim().isNotEmpty) return messages;
        }
      }
    }
    return null;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('Invite Guests'),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(
            AppDimens.space20,
            AppDimens.space16,
            AppDimens.space20,
            AppDimens.space32,
          ),
          children: [
            // Info banner
            _buildInfoBanner(),
            const SizedBox(height: AppDimens.space20),

            // File selector
            _buildFileSelector(),
            const SizedBox(height: AppDimens.space16),

            // Upload button
            if (_selectedFileBytes != null &&
                _state != _UploadState.uploading) ...[
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  style: AppButtonStyles.primary(),
                  onPressed: _upload,
                  icon: const Icon(Icons.upload_rounded, size: 18),
                  label: const Text('Send Registration Links'),
                ),
              ),
              const SizedBox(height: AppDimens.space8),
              SizedBox(
                width: double.infinity,
                child: TextButton(
                  onPressed: _reset,
                  child: const Text('Clear selection'),
                ),
              ),
            ],

            // Loading indicator
            if (_state == _UploadState.uploading) ...[
              const SizedBox(height: AppDimens.space24),
              Center(
                child: Column(
                  children: [
                    CircularProgressIndicator(
                      value: _uploadProgress,
                      valueColor: const AlwaysStoppedAnimation<Color>(
                          AppColors.obsidianBlack),
                    ),
                    const SizedBox(height: 16),
                    const Text(
                      'Sending registration links…',
                      style: TextStyle(
                        color: AppColors.textSecondary,
                        fontSize: 14,
                      ),
                    ),
                  ],
                ),
              ),
            ],

            // Error state
            if (_state == _UploadState.error && _error != null) ...[
              const SizedBox(height: AppDimens.space16),
              _buildErrorCard(),
            ],

            // Success result
            if (_state == _UploadState.success && _result != null) ...[
              const SizedBox(height: AppDimens.space16),
              _buildResultCard(_result!),
              const SizedBox(height: AppDimens.space16),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  style: AppButtonStyles.create(),
                  onPressed: _reset,
                  child: const Text('Upload Another File'),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildInfoBanner() {
    return const PastelCard(
      backgroundColor: AppColors.pastelBlueLight,
      padding: EdgeInsets.all(AppDimens.space16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          PastelIconBadge(
            icon: Icons.info_outline_rounded,
            variant: PastelIconVariant.blue,
            size: 40,
            iconSize: 18,
          ),
          SizedBox(width: AppDimens.space12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Supported formats: CSV, PDF, DOCX',
                  style: TextStyle(
                    color: AppColors.pastelBlueText,
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                SizedBox(height: 4),
                Text(
                  'Maximum file size: 5 MB. Valid guests receive a registration link by email. Duplicate emails in the file are skipped.',
                  style: TextStyle(
                    color: AppColors.pastelBlueText,
                    fontSize: 12,
                    height: 1.4,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFileSelector() {
    if (_selectedFileName != null && _selectedFileBytes != null) {
      return PastelCard(
        padding: const EdgeInsets.all(AppDimens.space16),
        child: Row(
          children: [
            Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(
                color: AppColors.pastelGreenLight,
                borderRadius: BorderRadius.circular(AppDimens.radiusMedium),
              ),
              child: const Icon(
                Icons.insert_drive_file_outlined,
                color: AppColors.pastelGreenText,
                size: 22,
              ),
            ),
            const SizedBox(width: AppDimens.space12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    _selectedFileName!,
                    style: const TextStyle(
                      color: AppColors.textPrimary,
                      fontSize: 14,
                      fontWeight: FontWeight.w700,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 2),
                  Text(
                    '${(_selectedFileSize! / 1024).toStringAsFixed(1)} KB',
                    style: const TextStyle(
                      color: AppColors.textSecondary,
                      fontSize: 12,
                    ),
                  ),
                ],
              ),
            ),
            const PastelPillBadge(
              text: 'Selected',
              style: PastelBadgeStyle.green,
              fontSize: 11,
            ),
          ],
        ),
      );
    }

    return GestureDetector(
      onTap: _pickFile,
      child: const DottedBorderBox(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            PastelIconBadge(
              icon: Icons.upload_file_rounded,
              variant: PastelIconVariant.lavender,
              size: 64,
              iconSize: 30,
              isCircle: false,
            ),
            SizedBox(height: AppDimens.space16),
            Text(
              'Tap to select a file',
              style: TextStyle(
                color: AppColors.textPrimary,
                fontSize: 16,
                fontWeight: FontWeight.w700,
              ),
            ),
            SizedBox(height: AppDimens.space6),
            Text(
              'CSV, PDF, or DOCX · Max 5 MB',
              style: TextStyle(
                color: AppColors.textSecondary,
                fontSize: 13,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildErrorCard() {
    return PastelCard(
      backgroundColor: AppColors.errorBg,
      padding: const EdgeInsets.all(AppDimens.space16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.error_outline_rounded,
              color: AppColors.error, size: 22),
          const SizedBox(width: AppDimens.space12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Upload Failed',
                  style: TextStyle(
                    color: AppColors.error,
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  _error!,
                  style: const TextStyle(
                    color: AppColors.error,
                    fontSize: 13,
                    height: 1.4,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildResultCard(BulkUploadResult result) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Summary metrics
        PastelCard(
          backgroundColor: result.failedRows == 0 && result.duplicateRows == 0
              ? AppColors.pastelGreenLight
              : AppColors.pastelYellowLight,
          padding: const EdgeInsets.all(AppDimens.space16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(
                    result.failedRows == 0
                        ? Icons.check_circle_outline_rounded
                        : Icons.warning_amber_rounded,
                    color: result.failedRows == 0
                        ? AppColors.pastelGreenText
                        : AppColors.pastelYellowText,
                    size: 22,
                  ),
                  const SizedBox(width: 8),
                  Text(
                    result.failedRows == 0
                        ? 'Upload Complete'
                        : 'Upload Complete with Issues',
                    style: TextStyle(
                      color: result.failedRows == 0
                          ? AppColors.pastelGreenText
                          : AppColors.pastelYellowText,
                      fontSize: 16,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space16),
              Row(
                children: [
                  Expanded(
                    child: _statItem(
                      '${result.totalRows}',
                      'Total',
                      AppColors.textPrimary,
                    ),
                  ),
                  Expanded(
                    child: _statItem(
                      '${result.successfulRows}',
                      'Links queued',
                      AppColors.pastelGreenText,
                    ),
                  ),
                  Expanded(
                    child: _statItem(
                      '${result.duplicateRows}',
                      'Duplicates',
                      AppColors.pastelYellowText,
                    ),
                  ),
                  Expanded(
                    child: _statItem(
                      '${result.failedRows}',
                      'Failed',
                      AppColors.error,
                    ),
                  ),
                ],
              ),
              if (result.alreadyRegisteredRows > 0) ...[
                const SizedBox(height: AppDimens.space8),
                Text(
                  '${result.alreadyRegisteredRows} guest(s) already registered for this event.',
                  style: const TextStyle(
                    color: AppColors.textSecondary,
                    fontSize: 12,
                  ),
                ),
              ],
            ],
          ),
        ),

        // Row errors
        if (result.errors.isNotEmpty) ...[
          const SizedBox(height: AppDimens.space16),
          const Text(
            'Row Errors',
            style: TextStyle(
              color: AppColors.textPrimary,
              fontSize: 16,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: AppDimens.space8),
          PastelCard(
            padding: const EdgeInsets.all(AppDimens.space12),
            child: Column(
              children: result.errors.map((e) {
                return Padding(
                  padding:
                      const EdgeInsets.symmetric(vertical: AppDimens.space6),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: AppColors.errorBg,
                          borderRadius:
                              BorderRadius.circular(AppDimens.radiusPill),
                        ),
                        child: Text(
                          'Row ${e.rowNumber}',
                          style: const TextStyle(
                            color: AppColors.error,
                            fontSize: 11,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              e.field,
                              style: const TextStyle(
                                color: AppColors.textPrimary,
                                fontSize: 12,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            Text(
                              e.message,
                              style: const TextStyle(
                                color: AppColors.textSecondary,
                                fontSize: 12,
                                height: 1.3,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                );
              }).toList(),
            ),
          ),
        ],

        // Note about processing
        const SizedBox(height: AppDimens.space12),
        const Text(
          'Guests enter AI review after they submit the emailed registration form.',
          style: TextStyle(
            color: AppColors.textMuted,
            fontSize: 12,
            height: 1.4,
          ),
        ),
      ],
    );
  }

  Widget _statItem(String value, String label, Color color) {
    return Column(
      children: [
        Text(
          value,
          style: TextStyle(
            color: color,
            fontSize: 24,
            fontWeight: FontWeight.w800,
          ),
        ),
        Text(
          label,
          style: const TextStyle(
            color: AppColors.textSecondary,
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );
  }
}

/// Simple dotted border container for the file drop zone.
class DottedBorderBox extends StatelessWidget {
  final Widget child;

  const DottedBorderBox({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      height: 180,
      decoration: BoxDecoration(
        color: AppColors.surfacePure,
        borderRadius: BorderRadius.circular(AppDimens.radiusCard),
        border: Border.all(
          color: AppColors.borderMuted,
          width: 1.5,
        ),
      ),
      child: Center(child: child),
    );
  }
}

/// File type selection bottom sheet.
class _FileTypeSheet extends StatelessWidget {
  final VoidCallback onSelectFile;

  const _FileTypeSheet({required this.onSelectFile});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(AppDimens.space20, AppDimens.space24,
          AppDimens.space20, AppDimens.space32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Select File',
            style: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: AppDimens.space4),
          const Text(
            'Supported: CSV, PDF, DOCX (max 5 MB)',
            style: TextStyle(color: AppColors.textSecondary, fontSize: 13),
          ),
          const SizedBox(height: AppDimens.space20),
          Material(
            color: Colors.transparent,
            child: ListTile(
              leading: const PastelIconBadge(
                icon: Icons.photo_library_outlined,
                variant: PastelIconVariant.blue,
                size: 44,
                iconSize: 22,
              ),
              title: const Text('Pick from Files / Gallery',
                  style: TextStyle(fontWeight: FontWeight.w700)),
              subtitle: const Text('Select a CSV, PDF, or DOCX file'),
              onTap: onSelectFile,
              contentPadding: EdgeInsets.zero,
            ),
          ),
          const SizedBox(height: AppDimens.space8),
          const Padding(
            padding: EdgeInsets.symmetric(horizontal: 4),
            child: Text(
              'Choose a CSV, PDF, or DOCX file from your device.',
              style: TextStyle(
                color: AppColors.textMuted,
                fontSize: 12,
                height: 1.4,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
