import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../api/vendor_remote_datasource.dart';
import '../models/vendor_gallery_image_model.dart';
import '../models/vendor_profile_model.dart';
import '../models/vendor_service_model.dart';

class VendorDashboardOverview extends StatefulWidget {
  const VendorDashboardOverview({super.key, required this.session});

  final AuthResponseModel session;

  @override
  State<VendorDashboardOverview> createState() =>
      _VendorDashboardOverviewState();
}

class _VendorDashboardOverviewState extends State<VendorDashboardOverview> {
  final _api = VendorRemoteDataSource();
  bool _loading = true;
  String? _error;
  VendorProfileModel? _profile;
  List<VendorServiceModel> _services = [];
  List<VendorGalleryImageModel> _gallery = [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final token = widget.session.accessToken;
    if (token.isEmpty) {
      setState(() {
        _loading = false;
        _error = 'Missing access token.';
      });
      return;
    }

    try {
      final profile = await _api.getMyProfile(token);
      List<VendorServiceModel> services = [];
      List<VendorGalleryImageModel> gallery = [];
      if (profile != null) {
        services = await _api.listMyServices(token);
        gallery = await _api.listGalleryImages(token);
      }
      if (!mounted) return;
      setState(() {
        _profile = profile;
        _services = services;
        _gallery = gallery;
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
    if (_loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppDimens.space16),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (_error != null) {
      return PastelCard(
        padding: const EdgeInsets.all(AppDimens.space16),
        child: Text(_error!, style: const TextStyle(color: AppColors.error)),
      );
    }

    if (_profile == null) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const PastelSectionHeader(title: 'Vendor overview'),
          PastelCard(
            padding: const EdgeInsets.all(AppDimens.space20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Set up your vendor profile to start listing services.',
                  style: TextStyle(color: AppColors.textSecondary),
                ),
                const SizedBox(height: AppDimens.space12),
                ElevatedButton(
                  onPressed: () => Navigator.of(context).pushNamed(
                    '/vendors/profile',
                    arguments: widget.session,
                  ),
                  child: const Text('Create profile'),
                ),
              ],
            ),
          ),
        ],
      );
    }

    final imageUrl =
        VendorRemoteDataSource.resolveImageUrl(_profile!.profileImageUrl);
    final recent = _services.take(3).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const PastelSectionHeader(title: 'Vendor overview'),
        PastelCard(
          padding: const EdgeInsets.all(AppDimens.space20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  CircleAvatar(
                    radius: 30,
                    backgroundColor: AppColors.pastelGreenLight,
                    backgroundImage:
                        imageUrl != null ? NetworkImage(imageUrl) : null,
                    child: imageUrl == null
                        ? const Icon(Icons.storefront_rounded,
                            color: AppColors.pastelGreenText)
                        : null,
                  ),
                  const SizedBox(width: AppDimens.space14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          _profile!.businessName,
                          style: const TextStyle(
                            fontWeight: FontWeight.w800,
                            fontSize: 18,
                          ),
                        ),
                        Text(
                          '${_profile!.category} · ${_profile!.status}',
                          style: const TextStyle(
                            color: AppColors.textSecondary,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        Text(
                          '${_profile!.contactEmail} · ${_profile!.contactPhone}',
                          style: const TextStyle(
                            color: AppColors.textMuted,
                            fontSize: 12,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space14),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () => Navigator.of(context).pushNamed(
                        '/vendors/profile',
                        arguments: widget.session,
                      ),
                      child: const Text('Edit Profile'),
                    ),
                  ),
                  const SizedBox(width: AppDimens.space8),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: () => Navigator.of(context).pushNamed(
                        '/vendors/services',
                        arguments: widget.session,
                      ),
                      child: const Text('Manage Services'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
        const PastelSectionHeader(title: 'Business images'),
        PastelCard(
          padding: const EdgeInsets.all(AppDimens.space16),
          child: _gallery.isEmpty
              ? const Text(
                  'No business images yet. Add them from Edit Profile.',
                  style: TextStyle(color: AppColors.textSecondary),
                )
              : Wrap(
                  spacing: 10,
                  runSpacing: 10,
                  children: _gallery.map((image) {
                    final url = VendorRemoteDataSource.resolveImageUrl(
                        image.imageUrl);
                    return ClipRRect(
                      borderRadius: BorderRadius.circular(14),
                      child: url == null
                          ? Container(
                              width: 110,
                              height: 84,
                              color: AppColors.pastelGreenLight,
                            )
                          : Image.network(
                              url,
                              width: 110,
                              height: 84,
                              fit: BoxFit.cover,
                            ),
                    );
                  }).toList(),
                ),
        ),
        const PastelSectionHeader(title: 'Services summary'),
        PastelCard(
          padding: const EdgeInsets.all(AppDimens.space20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                '${_services.length} service${_services.length == 1 ? '' : 's'} listed',
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: AppDimens.space12),
              if (recent.isEmpty)
                const Text(
                  'No services yet.',
                  style: TextStyle(color: AppColors.textSecondary),
                )
              else
                ...recent.map(
                  (service) => Padding(
                    padding: const EdgeInsets.only(bottom: AppDimens.space10),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          service.serviceName,
                          style: const TextStyle(fontWeight: FontWeight.w700),
                        ),
                        Text(
                          service.description ?? 'No description',
                          style: const TextStyle(
                            color: AppColors.textSecondary,
                            fontSize: 13,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              TextButton(
                onPressed: () => Navigator.of(context).pushNamed(
                  '/vendors/services',
                  arguments: widget.session,
                ),
                child: const Text('Manage Services'),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
