import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../core/theme/app_theme.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_icon_badge.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
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
        padding: EdgeInsets.symmetric(vertical: AppDimens.space24),
        child: Center(
          child: CircularProgressIndicator(
            valueColor: AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
          ),
        ),
      );
    }

    if (_error != null) {
      return PastelCard(
        padding: const EdgeInsets.all(AppDimens.space16),
        child: Row(
          children: [
            const Icon(Icons.error_outline, color: AppColors.error),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                _error!,
                style: const TextStyle(color: AppColors.error),
              ),
            ),
          ],
        ),
      );
    }

    if (_profile == null) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const PastelSectionHeader(title: 'Vendor setup'),
          PastelCard(
            padding: const EdgeInsets.all(AppDimens.space20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  children: [
                    PastelIconBadge(
                      icon: Icons.storefront_rounded,
                      variant: PastelIconVariant.olive,
                      size: 48,
                      iconSize: 24,
                    ),
                    SizedBox(width: 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Create your business profile',
                            style: TextStyle(
                              color: AppColors.textPrimary,
                              fontSize: 16,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          SizedBox(height: 2),
                          Text(
                            'Set up your vendor profile to start listing services and receiving quotes.',
                            style: TextStyle(
                              color: AppColors.textSecondary,
                              fontSize: 13,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppDimens.space16),
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton.icon(
                    style: AppButtonStyles.create(),
                    onPressed: () => Navigator.of(context).pushNamed(
                      '/vendors/profile',
                      arguments: widget.session,
                    ),
                    icon: const Icon(Icons.add_business_rounded),
                    label: const Text('Create Profile'),
                  ),
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
        // Vendor Metrics Grid
        const PastelSectionHeader(title: 'Business overview'),
        Row(
          children: [
            Expanded(
              child: _buildMetricCard(
                title: 'Services',
                value: '${_services.length}',
                subtitle: 'Active offerings',
                icon: Icons.handyman_rounded,
                bgColor: AppColors.pastelBlueLight,
                textColor: AppColors.pastelBlueText,
                onTap: () => Navigator.of(context).pushNamed(
                  '/vendors/services',
                  arguments: widget.session,
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildMetricCard(
                title: 'Requests',
                value: 'Quotes',
                subtitle: 'Inquiries',
                icon: Icons.request_quote_rounded,
                bgColor: AppColors.pastelYellowLight,
                textColor: AppColors.pastelYellowText,
                onTap: () => Navigator.of(context).pushNamed(
                  '/vendors/quotations',
                  arguments: widget.session,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: _buildMetricCard(
                title: 'Availability',
                value: 'Schedule',
                subtitle: 'Manage dates',
                icon: Icons.schedule_rounded,
                bgColor: AppColors.pastelLavenderLight,
                textColor: AppColors.pastelLavenderText,
                onTap: () => Navigator.of(context).pushNamed(
                  '/vendors/availability',
                  arguments: widget.session,
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildMetricCard(
                title: 'Status',
                value: _profile!.status,
                subtitle: _profile!.category,
                icon: Icons.verified_rounded,
                bgColor: AppColors.pastelGreenLight,
                textColor: AppColors.pastelGreenText,
                onTap: () => Navigator.of(context).pushNamed(
                  '/vendors/profile',
                  arguments: widget.session,
                ),
              ),
            ),
          ],
        ),

        const SizedBox(height: AppDimens.space14),

        // Business Profile Summary Card
        PastelCard(
          padding: const EdgeInsets.all(AppDimens.space20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  CircleAvatar(
                    radius: 28,
                    backgroundColor: AppColors.pastelGreenLight,
                    child: imageUrl == null
                        ? const Icon(
                            Icons.storefront_rounded,
                            color: AppColors.pastelGreenText,
                            size: 26,
                          )
                        : ClipOval(
                            child: Image.network(
                              imageUrl,
                              width: 56,
                              height: 56,
                              fit: BoxFit.cover,
                              errorBuilder: (context, error, stackTrace) =>
                                  const Icon(
                                Icons.storefront_rounded,
                                color: AppColors.pastelGreenText,
                                size: 26,
                              ),
                            ),
                          ),
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
                            letterSpacing: -0.3,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          '${_profile!.category} · ${_profile!.status}',
                          style: const TextStyle(
                            color: AppColors.textSecondary,
                            fontWeight: FontWeight.w600,
                            fontSize: 13,
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
              const SizedBox(height: AppDimens.space16),

              // Action Buttons Row with pastel styling
              Row(
                children: [
                  Expanded(
                    child: ElevatedButton.icon(
                      style: AppButtonStyles.primary(),
                      onPressed: () => Navigator.of(context).pushNamed(
                        '/vendors/services',
                        arguments: widget.session,
                      ),
                      icon: const Icon(Icons.handyman_outlined, size: 16),
                      label: const Text('Services'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: ElevatedButton.icon(
                      style: AppButtonStyles.warning(),
                      onPressed: () => Navigator.of(context).pushNamed(
                        '/vendors/quotations',
                        arguments: widget.session,
                      ),
                      icon: const Icon(Icons.request_quote_outlined, size: 16),
                      label: const Text('Requests'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      style: AppButtonStyles.outline(),
                      onPressed: () => Navigator.of(context).pushNamed(
                        '/vendors/profile',
                        arguments: widget.session,
                      ),
                      icon: const Icon(Icons.edit_outlined, size: 16),
                      label: const Text('Edit Profile'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: ElevatedButton.icon(
                      style: AppButtonStyles.create(),
                      onPressed: () => Navigator.of(context).pushNamed(
                        '/vendors/availability',
                        arguments: widget.session,
                      ),
                      icon: const Icon(Icons.event_available_outlined, size: 16),
                      label: const Text('Availability'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space8),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: () => Navigator.of(context).pushNamed(
                    '/vendors/analytics',
                    arguments: widget.session,
                  ),
                  child: const Text('View analytics'),
                ),
              ),
            ],
          ),
        ),

        // Services Summary Section
        const PastelSectionHeader(title: 'Services summary'),
        PastelCard(
          padding: const EdgeInsets.all(AppDimens.space20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    '${_services.length} service${_services.length == 1 ? '' : 's'} listed',
                    style: const TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 15,
                    ),
                  ),
                  PastelPillBadge(
                    text: '${_services.length}',
                    style: PastelBadgeStyle.blue,
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space12),
              if (recent.isEmpty)
                const Padding(
                  padding: EdgeInsets.symmetric(vertical: 8),
                  child: Text(
                    'No services listed yet. Add your services so planners can request quotations.',
                    style: TextStyle(color: AppColors.textSecondary),
                  ),
                )
              else
                ...recent.map(
                  (service) => Padding(
                    padding: const EdgeInsets.only(bottom: AppDimens.space10),
                    child: Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: AppColors.surfaceMuted,
                        borderRadius:
                            BorderRadius.circular(AppDimens.radiusMedium),
                      ),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  service.serviceName,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w700,
                                    fontSize: 14,
                                  ),
                                ),
                                if (service.description != null &&
                                    service.description!.isNotEmpty)
                                  Text(
                                    service.description!,
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                    style: const TextStyle(
                                      color: AppColors.textSecondary,
                                      fontSize: 12,
                                    ),
                                  ),
                              ],
                            ),
                          ),
                          Text(
                            service.displayPrice,
                            style: const TextStyle(
                              fontWeight: FontWeight.w700,
                              fontSize: 13,
                              color: AppColors.textPrimary,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              const SizedBox(height: AppDimens.space8),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  style: AppButtonStyles.primary(),
                  onPressed: () => Navigator.of(context).pushNamed(
                    '/vendors/services',
                    arguments: widget.session,
                  ),
                  icon: const Icon(Icons.add_rounded, size: 18),
                  label: const Text('Manage & Add Services'),
                ),
              ),
            ],
          ),
        ),

        // Business Images Section
        if (_gallery.isNotEmpty) ...[
          const PastelSectionHeader(title: 'Business images'),
          PastelCard(
            padding: const EdgeInsets.all(AppDimens.space16),
            child: Wrap(
              spacing: 10,
              runSpacing: 10,
              children: _gallery.map((image) {
                final url =
                    VendorRemoteDataSource.resolveImageUrl(image.imageUrl);
                return ClipRRect(
                  borderRadius: BorderRadius.circular(14),
                  child: url == null
                      ? Container(
                          width: 100,
                          height: 80,
                          color: AppColors.pastelGreenLight,
                        )
                      : Image.network(
                          url,
                          width: 100,
                          height: 80,
                          fit: BoxFit.cover,
                          errorBuilder: (context, error, stackTrace) =>
                              Container(
                            width: 100,
                            height: 80,
                            color: AppColors.pastelGreenLight,
                            child: const Icon(
                              Icons.image_outlined,
                              color: AppColors.pastelGreenText,
                            ),
                          ),
                        ),
                );
              }).toList(),
            ),
          ),
        ],
      ],
    );
  }

  Widget _buildMetricCard({
    required String title,
    required String value,
    required String subtitle,
    required IconData icon,
    required Color bgColor,
    required Color textColor,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(AppDimens.radiusCard),
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: bgColor,
          borderRadius: BorderRadius.circular(AppDimens.radiusCard),
          border: Border.all(color: AppColors.borderSubtle),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Icon(icon, color: textColor, size: 22),
                const Icon(
                  Icons.arrow_forward_ios_rounded,
                  size: 12,
                  color: AppColors.textMuted,
                ),
              ],
            ),
            const SizedBox(height: 10),
            Text(
              value,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: textColor,
                fontSize: 18,
                fontWeight: FontWeight.w800,
                letterSpacing: -0.5,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              title,
              style: const TextStyle(
                color: AppColors.textPrimary,
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
            Text(
              subtitle,
              style: const TextStyle(
                color: AppColors.textMuted,
                fontSize: 10,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
