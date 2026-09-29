import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

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

/// QR Check-in screen for event-day usage.
/// The backend is the sole source of truth for check-in validation.
/// Flutter sends the token to the backend and displays the result.
class QrCheckInPage extends StatefulWidget {
  final AuthResponseModel auth;
  final EventModel event;

  const QrCheckInPage({
    super.key,
    required this.auth,
    required this.event,
  });

  @override
  State<QrCheckInPage> createState() => _QrCheckInPageState();
}

enum _CheckInState { idle, loading, success, error }

class _QrCheckInPageState extends State<QrCheckInPage> {
  late final GuestManagementRemoteDataSource _api;
  final _tokenController = TextEditingController();
  final _focusNode = FocusNode();
  
  // Mobile scanner controller
  late final MobileScannerController _scannerController;
  bool _isProcessingScan = false;

  _CheckInState _state = _CheckInState.idle;
  CheckInResultModel? _result;
  String? _error;

  @override
  void initState() {
    super.initState();
    _api = GuestManagementRemoteDataSource();
    _scannerController = MobileScannerController(
      formats: const [BarcodeFormat.qrCode],
      detectionSpeed: DetectionSpeed.noDuplicates,
    );
  }

  @override
  void dispose() {
    _tokenController.dispose();
    _focusNode.dispose();
    _scannerController.dispose();
    super.dispose();
  }

  void _onDetect(BarcodeCapture capture) {
    if (_state != _CheckInState.idle || _isProcessingScan) return;
    final barcodes = capture.barcodes;
    if (barcodes.isNotEmpty) {
      final code = barcodes.first.rawValue;
      if (code != null && code.trim().isNotEmpty) {
        _isProcessingScan = true;
        _tokenController.text = code.trim();
        _checkIn(code);
      }
    }
  }

  Future<void> _checkIn(String token) async {
    final t = token.trim();
    if (t.isEmpty) {
      _isProcessingScan = false;
      return;
    }
    FocusScope.of(context).unfocus();

    setState(() {
      _state = _CheckInState.loading;
      _error = null;
      _result = null;
    });

    try {
      final result = await _api.checkIn(widget.event.id, t);
      if (mounted) {
        setState(() {
          _result = result;
          _state = _CheckInState.success;
        });
        _tokenController.clear();
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = _parseCheckInError(e);
          _state = _CheckInState.error;
        });
      }
    } finally {
      _isProcessingScan = false;
    }
  }

  String _parseCheckInError(Object e) {
    final msg = e.toString();
    // Try to extract JSON message
    final jsonMatch =
        RegExp(r'"(?:message|error)"\s*:\s*"([^"]+)"').firstMatch(msg);
    if (jsonMatch != null) return jsonMatch.group(1)!;
    if (msg.contains('400')) return 'Invalid QR token format.';
    if (msg.contains('401')) return 'Session expired. Please log in again.';
    if (msg.contains('403')) return 'You do not have permission to check in guests.';
    if (msg.contains('404')) return 'Invalid or expired QR code. This token was not found.';
    if (msg.contains('409')) return 'This guest has already been checked in.';
    if (msg.contains('422')) return 'This QR code is not valid for this event.';
    if (msg.contains('SocketException') || msg.contains('connection')) {
      return 'Network error. Please check your connection.';
    }
    return 'Check-in failed. Please verify the QR code and try again.';
  }

  void _reset() {
    setState(() {
      _state = _CheckInState.idle;
      _result = null;
      _error = null;
    });
    _tokenController.clear();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _focusNode.requestFocus();
    });
  }

  Future<void> _pasteFromClipboard() async {
    final data = await Clipboard.getData(Clipboard.kTextPlain);
    if (data?.text != null && mounted) {
      _tokenController.text = data!.text!.trim();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title: const Text('QR Check-In'),
        actions: [
          if (_state == _CheckInState.success || _state == _CheckInState.error)
            TextButton(
              onPressed: _reset,
              child: const Text('Scan Next'),
            ),
          const SizedBox(width: AppDimens.space8),
        ],
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(
            AppDimens.space20,
            AppDimens.space16,
            AppDimens.space20,
            AppDimens.space32,
          ),
          child: Column(
            children: [
              // Event context card
              PastelCard(
                padding: const EdgeInsets.all(AppDimens.space14),
                child: Row(
                  children: [
                    const PastelIconBadge(
                      icon: Icons.event_available_rounded,
                      variant: PastelIconVariant.green,
                      size: 40,
                      iconSize: 20,
                    ),
                    const SizedBox(width: AppDimens.space12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            widget.event.eventName.isNotEmpty
                                ? widget.event.eventName
                                : 'Event',
                            style: const TextStyle(
                              color: AppColors.textPrimary,
                              fontSize: 15,
                              fontWeight: FontWeight.w700,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                          const Text(
                            'Check-In Mode',
                            style: TextStyle(
                              color: AppColors.textSecondary,
                              fontSize: 12,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const PastelPillBadge(
                      text: 'LIVE',
                      style: PastelBadgeStyle.green,
                      icon: Icons.circle,
                      fontSize: 11,
                    ),
                  ],
                ),
              ),

              const SizedBox(height: AppDimens.space20),

              // QR Scanner / manual entry
              Expanded(
                child: _state == _CheckInState.loading
                    ? _buildLoadingState()
                    : _state == _CheckInState.success
                        ? _buildSuccessState()
                        : _state == _CheckInState.error
                            ? _buildErrorState()
                            : _buildInputState(),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildInputState() {
    return Column(
      children: [
        // Camera scanner
        Container(
          height: 240,
          width: double.infinity,
          decoration: BoxDecoration(
            color: Colors.black,
            borderRadius: BorderRadius.circular(AppDimens.radiusCard),
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(AppDimens.radiusCard),
            child: Stack(
              fit: StackFit.expand,
              children: [
                MobileScanner(
                  controller: _scannerController,
                  onDetect: _onDetect,
                  errorBuilder: (context, error, child) {
                    return const Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.videocam_off_rounded, color: Colors.white54, size: 48),
                          SizedBox(height: 8),
                          Text('Camera unavailable', style: TextStyle(color: Colors.white70)),
                        ],
                      ),
                    );
                  },
                ),
                // Corner brackets (QR scanner UX)
                ..._buildScannerCorners(),
                // Scan line animation or instruction
                const Align(
                  alignment: Alignment.bottomCenter,
                  child: Padding(
                    padding: EdgeInsets.only(bottom: 16),
                    child: Text(
                      'Scan guest QR code',
                      style: TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.w600,
                        shadows: [Shadow(color: Colors.black54, blurRadius: 4)],
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: AppDimens.space20),
        const Row(
          children: [
            Expanded(child: Divider()),
            Padding(
              padding: EdgeInsets.symmetric(horizontal: 12),
              child: Text(
                'OR enter token manually',
                style: TextStyle(
                  color: AppColors.textMuted,
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            Expanded(child: Divider()),
          ],
        ),
        const SizedBox(height: AppDimens.space16),
        // Manual token entry
        Row(
          children: [
            Expanded(
              child: TextField(
                controller: _tokenController,
                focusNode: _focusNode,
                decoration: InputDecoration(
                  hintText: 'Paste or type QR token…',
                  prefixIcon: const Icon(
                    Icons.qr_code_rounded,
                    size: 20,
                    color: AppColors.textMuted,
                  ),
                  suffixIcon: IconButton(
                    tooltip: 'Paste from clipboard',
                    icon: const Icon(Icons.content_paste_rounded,
                        size: 18, color: AppColors.textMuted),
                    onPressed: _pasteFromClipboard,
                  ),
                ),
                onSubmitted: _checkIn,
                textInputAction: TextInputAction.done,
              ),
            ),
          ],
        ),
        const SizedBox(height: AppDimens.space12),
        SizedBox(
          width: double.infinity,
          child: ElevatedButton.icon(
            style: AppButtonStyles.success(),
            onPressed: () => _checkIn(_tokenController.text),
            icon: const Icon(Icons.login_rounded, size: 18),
            label: const Text('Check In Guest'),
          ),
        ),
      ],
    );
  }

  Widget _buildLoadingState() {
    return const Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(
            width: 56,
            height: 56,
            child: CircularProgressIndicator(
              strokeWidth: 3,
              valueColor:
                  AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
            ),
          ),
          SizedBox(height: 20),
          Text(
            'Verifying with server…',
            style: TextStyle(
              color: AppColors.textSecondary,
              fontSize: 16,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSuccessState() {
    final guest = _result!.guest;
    return Column(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Container(
          width: 80,
          height: 80,
          decoration: const BoxDecoration(
            color: AppColors.pastelGreen,
            shape: BoxShape.circle,
          ),
          child: const Icon(
            Icons.check_rounded,
            color: AppColors.pastelGreenText,
            size: 44,
          ),
        ),
        const SizedBox(height: AppDimens.space20),
        const Text(
          'Check-In Successful!',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontSize: 22,
            fontWeight: FontWeight.w800,
            letterSpacing: -0.4,
          ),
        ),
        const SizedBox(height: AppDimens.space8),
        Text(
          guest.fullName,
          style: const TextStyle(
            color: AppColors.textSecondary,
            fontSize: 16,
            fontWeight: FontWeight.w600,
          ),
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AppDimens.space4),
        Text(
          guest.emailAddress,
          style: const TextStyle(
            color: AppColors.textMuted,
            fontSize: 13,
          ),
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AppDimens.space20),
        // Check-in details
        PastelCard(
          padding: const EdgeInsets.all(AppDimens.space16),
          child: Column(
            children: [
              if (guest.checkedInAt != null)
                _resultInfoRow(
                  icon: Icons.schedule_outlined,
                  label: 'Checked In At',
                  value: _formatDate(guest.checkedInAt!),
                ),
              if (guest.checkedInMethod != null) ...[
                const SizedBox(height: 8),
                _resultInfoRow(
                  icon: Icons.qr_code_rounded,
                  label: 'Method',
                  value: guest.checkedInMethod!,
                ),
              ],
              const SizedBox(height: 8),
              _resultInfoRow(
                icon: Icons.event_outlined,
                label: 'Event',
                value: widget.event.eventName,
              ),
            ],
          ),
        ),
        const SizedBox(height: AppDimens.space24),
        SizedBox(
          width: double.infinity,
          child: ElevatedButton.icon(
            style: AppButtonStyles.success(),
            onPressed: _reset,
            icon: const Icon(Icons.qr_code_scanner_rounded, size: 18),
            label: const Text('Scan Next Guest'),
          ),
        ),
      ],
    );
  }

  Widget _buildErrorState() {
    return Column(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Container(
          width: 80,
          height: 80,
          decoration: const BoxDecoration(
            color: AppColors.errorBg,
            shape: BoxShape.circle,
          ),
          child: const Icon(
            Icons.close_rounded,
            color: AppColors.error,
            size: 44,
          ),
        ),
        const SizedBox(height: AppDimens.space20),
        const Text(
          'Check-In Failed',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontSize: 22,
            fontWeight: FontWeight.w800,
            letterSpacing: -0.4,
          ),
        ),
        const SizedBox(height: AppDimens.space12),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppDimens.space16),
          child: Text(
            _error ?? 'An error occurred.',
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: AppColors.textSecondary,
              fontSize: 15,
              height: 1.4,
            ),
          ),
        ),
        const SizedBox(height: AppDimens.space24),
        SizedBox(
          width: double.infinity,
          child: ElevatedButton.icon(
            onPressed: _reset,
            icon: const Icon(Icons.refresh_rounded, size: 18),
            label: const Text('Try Again'),
          ),
        ),
      ],
    );
  }

  Widget _resultInfoRow({
    required IconData icon,
    required String label,
    required String value,
  }) {
    return Row(
      children: [
        Icon(icon, size: 16, color: AppColors.textMuted),
        const SizedBox(width: 8),
        Text(
          '$label: ',
          style: const TextStyle(
            color: AppColors.textSecondary,
            fontSize: 13,
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(
              color: AppColors.textPrimary,
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }

  String _formatDate(DateTime dt) {
    final local = dt.toLocal();
    return '${local.day.toString().padLeft(2, '0')}/'
        '${local.month.toString().padLeft(2, '0')}/'
        '${local.year}  '
        '${local.hour.toString().padLeft(2, '0')}:'
        '${local.minute.toString().padLeft(2, '0')}';
  }

  List<Widget> _buildScannerCorners() {
    const size = 32.0;
    const thickness = 3.0;
    const color = Colors.white;
    return [
      Positioned(
        top: 24, left: 24,
        child: CustomPaint(painter: _CornerPainter(size, thickness, color, CornerPos.topLeft)),
      ),
      Positioned(
        top: 24, right: 24,
        child: CustomPaint(painter: _CornerPainter(size, thickness, color, CornerPos.topRight)),
      ),
      Positioned(
        bottom: 24, left: 24,
        child: CustomPaint(painter: _CornerPainter(size, thickness, color, CornerPos.bottomLeft)),
      ),
      Positioned(
        bottom: 24, right: 24,
        child: CustomPaint(painter: _CornerPainter(size, thickness, color, CornerPos.bottomRight)),
      ),
    ];
  }
}

enum CornerPos { topLeft, topRight, bottomLeft, bottomRight }

class _CornerPainter extends CustomPainter {
  final double size;
  final double thickness;
  final Color color;
  final CornerPos pos;

  _CornerPainter(this.size, this.thickness, this.color, this.pos);

  @override
  void paint(Canvas canvas, Size _) {
    final paint = Paint()
      ..color = color
      ..strokeWidth = thickness
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    switch (pos) {
      case CornerPos.topLeft:
        canvas.drawLine(Offset.zero, Offset(size, 0), paint);
        canvas.drawLine(Offset.zero, Offset(0, size), paint);
        break;
      case CornerPos.topRight:
        canvas.drawLine(Offset.zero, Offset(-size, 0), paint);
        canvas.drawLine(Offset.zero, Offset(0, size), paint);
        break;
      case CornerPos.bottomLeft:
        canvas.drawLine(Offset.zero, Offset(size, 0), paint);
        canvas.drawLine(Offset.zero, Offset(0, -size), paint);
        break;
      case CornerPos.bottomRight:
        canvas.drawLine(Offset.zero, Offset(-size, 0), paint);
        canvas.drawLine(Offset.zero, Offset(0, -size), paint);
        break;
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
