import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../core/theme/app_theme.dart';
import '../../../shared/widgets/pastel_bottom_nav_bar.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/quotation_remote_datasource.dart';
import '../models/quotation_model.dart';

class VendorQuotationsPage extends StatefulWidget {
  const VendorQuotationsPage({super.key});

  @override
  State<VendorQuotationsPage> createState() => _VendorQuotationsPageState();
}

class _VendorQuotationsPageState extends State<VendorQuotationsPage> {
  final _api = QuotationRemoteDataSource();
  AuthResponseModel? _auth;
  List<QuotationModel> _items = [];
  bool _loading = true;
  String? _error;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_loading) {
      _load();
    }
  }

  Future<void> _load() async {
    final args = ModalRoute.of(context)?.settings.arguments;
    final auth = args is AuthResponseModel
        ? args
        : (args is Map ? args['auth'] : null);

    if (auth is! AuthResponseModel || auth.accessToken.isEmpty) {
      setState(() {
        _loading = false;
        _error = 'Missing session.';
      });
      return;
    }

    try {
      final items = await _api.listForVendor(auth.accessToken);
      if (!mounted) return;
      setState(() {
        _auth = auth;
        _items = items;
        _loading = false;
        _error = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = e.toString();
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(title: const Text('Quotation requests')),
      bottomNavigationBar: PastelBottomNavBar.roleBased(
        context: context,
        session: _auth,
        currentIndex: 2,
      ),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(
                valueColor:
                    AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
              ),
            )
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Text(_error!,
                        style: const TextStyle(color: AppColors.error)),
                  ),
                )
              : RefreshIndicator(
                  color: AppColors.obsidianBlack,
                  backgroundColor: AppColors.surfacePure,
                  onRefresh: _load,
                  child: ListView(
                    padding: const EdgeInsets.fromLTRB(
                      AppDimens.space20,
                      AppDimens.space12,
                      AppDimens.space20,
                      AppDimens.space32,
                    ),
                    children: [
                      const PastelSectionHeader(title: 'Incoming requests'),
                      if (_items.isEmpty)
                        const PastelCard(
                          padding: EdgeInsets.all(AppDimens.space24),
                          child: Center(
                            child: Column(
                              children: [
                                Icon(
                                  Icons.request_quote_outlined,
                                  size: 40,
                                  color: AppColors.textMuted,
                                ),
                                SizedBox(height: 12),
                                Text(
                                  'No quotation requests yet.',
                                  style: TextStyle(
                                    color: AppColors.textPrimary,
                                    fontWeight: FontWeight.w700,
                                    fontSize: 15,
                                  ),
                                ),
                                SizedBox(height: 4),
                                Text(
                                  'Requests submitted by event planners will appear here.',
                                  textAlign: TextAlign.center,
                                  style: TextStyle(
                                    color: AppColors.textSecondary,
                                    fontSize: 13,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        )
                      else
                        ..._items.map(
                          (q) => Padding(
                            padding: const EdgeInsets.only(
                                bottom: AppDimens.space12),
                            child: PastelCard(
                              padding: const EdgeInsets.all(AppDimens.space18),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    mainAxisAlignment:
                                        MainAxisAlignment.spaceBetween,
                                    children: [
                                      Expanded(
                                        child: Text(
                                          q.serviceName,
                                          style: const TextStyle(
                                            fontWeight: FontWeight.w800,
                                            fontSize: 16,
                                          ),
                                        ),
                                      ),
                                      PastelPillBadge(
                                        text: q.displayStatus.toUpperCase(),
                                        style: q.status == 'REQUESTED'
                                            ? PastelBadgeStyle.yellow
                                            : PastelBadgeStyle.green,
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    'Event: ${q.eventType ?? '—'} · ${q.guestCount ?? '—'} guests',
                                    style: const TextStyle(
                                      color: AppColors.textSecondary,
                                      fontSize: 13,
                                    ),
                                  ),
                                  const SizedBox(height: 2),
                                  Text(
                                    'Dates: ${q.displayRange}',
                                    style: const TextStyle(
                                      color: AppColors.textMuted,
                                      fontSize: 12,
                                    ),
                                  ),
                                  if (q.customerMessage != null &&
                                      q.customerMessage!.isNotEmpty) ...[
                                    const SizedBox(height: 6),
                                    Text(
                                      'Client note: ${q.customerMessage}',
                                      style: const TextStyle(
                                        color: AppColors.textSecondary,
                                        fontSize: 13,
                                      ),
                                    ),
                                  ],
                                  const SizedBox(height: 12),
                                  Align(
                                    alignment: Alignment.centerRight,
                                    child: ElevatedButton.icon(
                                      style: q.status == 'REQUESTED'
                                          ? AppButtonStyles.warning()
                                          : AppButtonStyles.primary(),
                                      onPressed: () => Navigator.of(context)
                                          .pushNamed(
                                        '/vendors/quotations/details',
                                        arguments: {
                                          'auth': _auth,
                                          'quotationId': q.id,
                                        },
                                      ).then((_) => _load()),
                                      icon: Icon(
                                        q.status == 'REQUESTED'
                                            ? Icons.reply_rounded
                                            : Icons.visibility_outlined,
                                        size: 16,
                                      ),
                                      label: Text(
                                        q.status == 'REQUESTED'
                                            ? 'Respond to Request'
                                            : 'View Details',
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                    ],
                  ),
                ),
    );
  }
}
