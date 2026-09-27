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

class MyQuotationsPage extends StatefulWidget {
  const MyQuotationsPage({super.key});

  @override
  State<MyQuotationsPage> createState() => _MyQuotationsPageState();
}

class _MyQuotationsPageState extends State<MyQuotationsPage> {
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
      final items = await _api.listMine(auth.accessToken);
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
      appBar: AppBar(title: const Text('My quotations')),
      bottomNavigationBar: PastelBottomNavBar.roleBased(
        context: context,
        session: _auth,
        currentIndex: 3,
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
                      const PastelSectionHeader(
                        title: 'Your quotation requests',
                      ),
                      if (_items.isEmpty)
                        PastelCard(
                          padding: const EdgeInsets.all(AppDimens.space24),
                          child: Column(
                            children: [
                              const Icon(
                                Icons.receipt_long_outlined,
                                size: 40,
                                color: AppColors.textMuted,
                              ),
                              const SizedBox(height: 12),
                              const Text(
                                'No quotation requests yet.',
                                style: TextStyle(
                                  color: AppColors.textPrimary,
                                  fontWeight: FontWeight.w700,
                                  fontSize: 15,
                                ),
                              ),
                              const SizedBox(height: 4),
                              const Text(
                                'Browse the marketplace to find vendors and request price quotes.',
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  color: AppColors.textSecondary,
                                  fontSize: 13,
                                ),
                              ),
                              const SizedBox(height: 16),
                              ElevatedButton.icon(
                                style: AppButtonStyles.primary(),
                                onPressed: () =>
                                    Navigator.of(context).pushNamed(
                                  '/marketplace',
                                  arguments: _auth,
                                ),
                                icon: const Icon(Icons.storefront_outlined,
                                    size: 16),
                                label: const Text('Browse marketplace'),
                              ),
                            ],
                          ),
                        )
                      else ...[
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
                                          q.vendorBusinessName,
                                          style: const TextStyle(
                                            fontWeight: FontWeight.w800,
                                            fontSize: 16,
                                          ),
                                        ),
                                      ),
                                      PastelPillBadge(
                                        text: q.displayStatus.toUpperCase(),
                                        style: q.displayStatus
                                                .toLowerCase()
                                                .contains('respond')
                                            ? PastelBadgeStyle.green
                                            : PastelBadgeStyle.yellow,
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 6),
                                  Text(
                                    q.serviceName,
                                    style: const TextStyle(
                                      fontWeight: FontWeight.w700,
                                      color: AppColors.textPrimary,
                                    ),
                                  ),
                                  if (q.eventType != null) ...[
                                    const SizedBox(height: 2),
                                    Text(
                                      'Event: ${q.eventType}',
                                      style: const TextStyle(
                                        color: AppColors.textSecondary,
                                        fontSize: 13,
                                      ),
                                    ),
                                  ],
                                  const SizedBox(height: 4),
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
                                      'Your note: ${q.customerMessage}',
                                      style: const TextStyle(
                                        color: AppColors.textSecondary,
                                        fontSize: 13,
                                      ),
                                    ),
                                  ],
                                  const Divider(height: 20),
                                  Row(
                                    mainAxisAlignment:
                                        MainAxisAlignment.spaceBetween,
                                    children: [
                                      const Text(
                                        'Quoted price:',
                                        style: TextStyle(
                                          color: AppColors.textSecondary,
                                          fontSize: 13,
                                        ),
                                      ),
                                      Text(
                                        q.displayQuotedPrice,
                                        style: const TextStyle(
                                          fontWeight: FontWeight.w800,
                                          fontSize: 15,
                                          color: AppColors.textPrimary,
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(height: 8),
                        SizedBox(
                          width: double.infinity,
                          child: ElevatedButton.icon(
                            style: AppButtonStyles.primary(),
                            onPressed: () => Navigator.of(context).pushNamed(
                              '/marketplace',
                              arguments: _auth,
                            ),
                            icon: const Icon(Icons.storefront_outlined,
                                size: 16),
                            label: const Text('Browse more vendors'),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
    );
  }
}
