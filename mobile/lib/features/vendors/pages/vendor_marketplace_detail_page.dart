import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/vendor_remote_datasource.dart';
import '../models/marketplace_vendor_model.dart';

class VendorMarketplaceDetailPage extends StatefulWidget {
  const VendorMarketplaceDetailPage({super.key});

  @override
  State<VendorMarketplaceDetailPage> createState() =>
      _VendorMarketplaceDetailPageState();
}

class _VendorMarketplaceDetailPageState
    extends State<VendorMarketplaceDetailPage> {
  final _vendorApi = VendorRemoteDataSource();
  MarketplaceVendorDetail? _vendor;
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
    String? token;
    String? vendorId;

    if (args is Map) {
      final auth = args['auth'];
      if (auth is AuthResponseModel) {
        token = auth.accessToken;
      }
      vendorId = args['vendorId']?.toString();
    }

    if (token == null || token.isEmpty || vendorId == null || vendorId.isEmpty) {
      setState(() {
        _loading = false;
        _error = 'Missing vendor details.';
      });
      return;
    }

    try {
      final vendor = await _vendorApi.getMarketplaceVendor(token, vendorId);
      if (!mounted) return;
      setState(() {
        _vendor = vendor;
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
    final vendor = _vendor;
    final logoUrl = vendor == null
        ? null
        : VendorRemoteDataSource.resolveImageUrl(vendor.profileImageUrl);

    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(title: const Text('Vendor details')),
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
                    child: Text(
                      _error!,
                      style: const TextStyle(
                        color: AppColors.error,
                        fontWeight: FontWeight.w600,
                      ),
                      textAlign: TextAlign.center,
                    ),
                  ),
                )
              : vendor == null
                  ? const SizedBox.shrink()
                  : ListView(
                      padding: const EdgeInsets.fromLTRB(
                        AppDimens.space20,
                        AppDimens.space12,
                        AppDimens.space20,
                        AppDimens.space32,
                      ),
                      children: [
                        PastelCard(
                          padding: const EdgeInsets.all(AppDimens.space20),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              if (logoUrl != null) ...[
                                ClipRRect(
                                  borderRadius: BorderRadius.circular(
                                    AppDimens.radiusMedium,
                                  ),
                                  child: Image.network(
                                    logoUrl,
                                    height: 120,
                                    width: double.infinity,
                                    fit: BoxFit.cover,
                                    errorBuilder: (_, _, _) =>
                                        const SizedBox.shrink(),
                                  ),
                                ),
                                const SizedBox(height: AppDimens.space12),
                              ],
                              Text(
                                vendor.businessName,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w800,
                                  fontSize: 22,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                vendor.category,
                                style: const TextStyle(
                                  color: AppColors.textSecondary,
                                ),
                              ),
                              const SizedBox(height: 10),
                              Text(vendor.description ??
                                  'No description provided.'),
                              const SizedBox(height: 8),
                              Text(
                                vendor.address,
                                style: const TextStyle(
                                  color: AppColors.textSecondary,
                                ),
                              ),
                              if (vendor.websiteUrl != null &&
                                  vendor.websiteUrl!.isNotEmpty) ...[
                                const SizedBox(height: 6),
                                Text(
                                  vendor.websiteUrl!,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ],
                            ],
                          ),
                        ),
                        const PastelSectionHeader(title: 'Business photos'),
                        if (vendor.images.isEmpty)
                          const PastelCard(
                            padding: EdgeInsets.all(AppDimens.space20),
                            child: Text('No gallery photos yet.'),
                          )
                        else
                          SizedBox(
                            height: 110,
                            child: ListView.separated(
                              scrollDirection: Axis.horizontal,
                              itemCount: vendor.images.length,
                              separatorBuilder: (_, _) =>
                                  const SizedBox(width: 10),
                              itemBuilder: (context, index) {
                                final imageUrl =
                                    VendorRemoteDataSource.resolveImageUrl(
                                  vendor.images[index].imageUrl,
                                );
                                return ClipRRect(
                                  borderRadius: BorderRadius.circular(
                                    AppDimens.radiusMedium,
                                  ),
                                  child: Image.network(
                                    imageUrl ?? '',
                                    width: 140,
                                    height: 110,
                                    fit: BoxFit.cover,
                                    errorBuilder: (_, _, _) => Container(
                                      width: 140,
                                      height: 110,
                                      color: AppColors.surfacePure,
                                      child: const Icon(Icons.image_outlined),
                                    ),
                                  ),
                                );
                              },
                            ),
                          ),
                        const PastelSectionHeader(title: 'Services'),
                        if (vendor.services.isEmpty)
                          const PastelCard(
                            padding: EdgeInsets.all(AppDimens.space20),
                            child: Text('No services listed.'),
                          )
                        else
                          ...vendor.services.map(
                            (service) => Padding(
                              padding: const EdgeInsets.only(
                                bottom: AppDimens.space12,
                              ),
                              child: PastelCard(
                                padding:
                                    const EdgeInsets.all(AppDimens.space16),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      service.serviceName,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w800,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      service.description ?? 'No description',
                                      style: const TextStyle(
                                        color: AppColors.textSecondary,
                                      ),
                                    ),
                                    const SizedBox(height: 6),
                                    Text(
                                      service.displayPrice,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w700,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                        const PastelSectionHeader(
                          title: 'Upcoming availability',
                        ),
                        if (vendor.availability.isEmpty)
                          const PastelCard(
                            padding: EdgeInsets.all(AppDimens.space20),
                            child: Text('No upcoming availability listed.'),
                          )
                        else
                          ...vendor.availability.map(
                            (period) => Padding(
                              padding: const EdgeInsets.only(
                                bottom: AppDimens.space8,
                              ),
                              child: PastelCard(
                                padding:
                                    const EdgeInsets.all(AppDimens.space16),
                                child: Text(
                                  '${period.isAvailable ? 'Available' : 'Unavailable'}: ${period.displayRange}',
                                ),
                              ),
                            ),
                          ),
                      ],
                    ),
    );
  }
}
