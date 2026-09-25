import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../../auth/widgets/custom_text_field.dart';
import '../api/vendor_remote_datasource.dart';
import '../models/marketplace_vendor_model.dart';

const _categories = [
  'CATERING',
  'PHOTOGRAPHY',
  'VENUE',
  'MUSIC',
  'FLORIST',
  'TRANSPORTATION',
];

class VendorMarketplacePage extends StatefulWidget {
  const VendorMarketplacePage({super.key});

  @override
  State<VendorMarketplacePage> createState() => _VendorMarketplacePageState();
}

class _VendorMarketplacePageState extends State<VendorMarketplacePage> {
  final _searchController = TextEditingController();
  final _vendorApi = VendorRemoteDataSource();

  List<MarketplaceVendorSummary> _items = [];
  String? _category;
  String _sortBy = 'businessName';
  String _sortOrder = 'asc';
  int _page = 1;
  int _totalPages = 0;
  bool _loading = true;
  String? _error;

  AuthResponseModel? get _auth =>
      ModalRoute.of(context)?.settings.arguments as AuthResponseModel?;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_loading) {
      _load();
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final token = _auth?.accessToken;
    if (token == null || token.isEmpty) {
      setState(() {
        _loading = false;
        _error = 'Please log in first.';
      });
      return;
    }

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final result = await _vendorApi.listMarketplace(
        token,
        search: _searchController.text.trim(),
        category: _category,
        sortBy: _sortBy,
        sortOrder: _sortOrder,
        page: _page,
        pageSize: 6,
      );
      if (!mounted) return;
      setState(() {
        _items = result.items;
        _totalPages = result.totalPages;
        _loading = false;
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
    final session = _auth;

    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(title: const Text('Vendor Marketplace')),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(
                valueColor:
                    AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
              ),
            )
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.fromLTRB(
                  AppDimens.space20,
                  AppDimens.space12,
                  AppDimens.space20,
                  AppDimens.space32,
                ),
                children: [
                  if (_error != null) ...[
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(AppDimens.space14),
                      decoration: BoxDecoration(
                        color: AppColors.errorBg,
                        borderRadius:
                            BorderRadius.circular(AppDimens.radiusMedium),
                      ),
                      child: Text(
                        _error!,
                        style: const TextStyle(
                          color: AppColors.error,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                    const SizedBox(height: AppDimens.space16),
                  ],
                  const PastelSectionHeader(title: 'Find vendors'),
                  PastelCard(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Column(
                      children: [
                        CustomTextField(
                          label: 'Search',
                          hint: 'Business name',
                          controller: _searchController,
                        ),
                        const SizedBox(height: AppDimens.space12),
                        DropdownButtonFormField<String>(
                          value: _category ?? '',
                          decoration: const InputDecoration(
                            labelText: 'Category',
                            border: OutlineInputBorder(),
                          ),
                          items: [
                            const DropdownMenuItem(
                              value: '',
                              child: Text('All'),
                            ),
                            ..._categories.map(
                              (category) => DropdownMenuItem(
                                value: category,
                                child: Text(category),
                              ),
                            ),
                          ],
                          onChanged: (value) {
                            setState(() {
                              _category =
                                  (value == null || value.isEmpty) ? null : value;
                            });
                          },
                        ),
                        const SizedBox(height: AppDimens.space12),
                        DropdownButtonFormField<String>(
                          value: _sortBy,
                          decoration: const InputDecoration(
                            labelText: 'Sort by',
                            border: OutlineInputBorder(),
                          ),
                          items: const [
                            DropdownMenuItem(
                              value: 'businessName',
                              child: Text('Business name'),
                            ),
                            DropdownMenuItem(
                              value: 'category',
                              child: Text('Category'),
                            ),
                            DropdownMenuItem(
                              value: 'createdAt',
                              child: Text('Newest'),
                            ),
                            DropdownMenuItem(
                              value: 'startingPrice',
                              child: Text('Starting price'),
                            ),
                          ],
                          onChanged: (value) {
                            if (value != null) {
                              setState(() => _sortBy = value);
                            }
                          },
                        ),
                        const SizedBox(height: AppDimens.space12),
                        SizedBox(
                          width: double.infinity,
                          height: 48,
                          child: ElevatedButton(
                            onPressed: () {
                              setState(() => _page = 1);
                              _load();
                            },
                            child: const Text('Search'),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const PastelSectionHeader(title: 'Approved vendors'),
                  if (_items.isEmpty)
                    const PastelCard(
                      padding: EdgeInsets.all(AppDimens.space20),
                      child: Text('No approved vendors match your filters.'),
                    )
                  else
                    ..._items.map(
                      (vendor) => Padding(
                        padding:
                            const EdgeInsets.only(bottom: AppDimens.space12),
                        child: PastelCard(
                          padding: const EdgeInsets.all(AppDimens.space16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                vendor.businessName,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w800,
                                  fontSize: 16,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                vendor.category,
                                style: const TextStyle(
                                  color: AppColors.textSecondary,
                                ),
                              ),
                              const SizedBox(height: 6),
                              Text(
                                vendor.shortDescription ??
                                    'No description provided.',
                              ),
                              const SizedBox(height: 4),
                              Text(
                                vendor.address,
                                style: const TextStyle(
                                  color: AppColors.textSecondary,
                                ),
                              ),
                              const SizedBox(height: 8),
                              Text(
                                vendor.displayStartingPrice,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              Align(
                                alignment: Alignment.centerRight,
                                child: TextButton(
                                  onPressed: () {
                                    Navigator.of(context).pushNamed(
                                      '/marketplace/details',
                                      arguments: {
                                        'auth': session,
                                        'vendorId': vendor.id,
                                      },
                                    );
                                  },
                                  child: const Text('View Vendor'),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  if (_totalPages > 1)
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        IconButton(
                          onPressed: _page > 1
                              ? () {
                                  setState(() => _page -= 1);
                                  _load();
                                }
                              : null,
                          icon: const Icon(Icons.chevron_left),
                        ),
                        Text('Page $_page of $_totalPages'),
                        IconButton(
                          onPressed: _page < _totalPages
                              ? () {
                                  setState(() => _page += 1);
                                  _load();
                                }
                              : null,
                          icon: const Icon(Icons.chevron_right),
                        ),
                      ],
                    ),
                ],
              ),
            ),
    );
  }
}
