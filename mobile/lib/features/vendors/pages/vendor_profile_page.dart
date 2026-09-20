import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../../auth/widgets/custom_text_field.dart';
import '../api/vendor_remote_datasource.dart';
import '../models/vendor_profile_model.dart';

const vendorCategories = [
  'CATERING',
  'PHOTOGRAPHY',
  'VENUE',
  'MUSIC',
  'FLORIST',
  'TRANSPORTATION',
];

class VendorProfilePage extends StatefulWidget {
  const VendorProfilePage({super.key});

  @override
  State<VendorProfilePage> createState() => _VendorProfilePageState();
}

class _VendorProfilePageState extends State<VendorProfilePage> {
  final _formKey = GlobalKey<FormState>();
  final _businessNameController = TextEditingController();
  final _contactEmailController = TextEditingController();
  final _contactPhoneController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _vendorApi = VendorRemoteDataSource();

  String _category = 'CATERING';
  bool _loading = true;
  bool _saving = false;
  bool _hasProfile = false;
  String? _status;
  String? _error;

  AuthResponseModel? get _auth =>
      ModalRoute.of(context)?.settings.arguments as AuthResponseModel?;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_loading) {
      _loadProfile();
    }
  }

  @override
  void dispose() {
    _businessNameController.dispose();
    _contactEmailController.dispose();
    _contactPhoneController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _loadProfile() async {
    final token = _auth?.accessToken;
    if (token == null || token.isEmpty) {
      setState(() {
        _loading = false;
        _error = 'Please log in as a vendor first.';
      });
      return;
    }

    try {
      final profile = await _vendorApi.getMyProfile(token);
      if (!mounted) return;
      if (profile != null) {
        _businessNameController.text = profile.businessName;
        _contactEmailController.text = profile.contactEmail;
        _contactPhoneController.text = profile.contactPhone;
        _descriptionController.text = profile.description ?? '';
        _category = vendorCategories.contains(profile.category)
            ? profile.category
            : 'CATERING';
        _status = profile.status;
        _hasProfile = true;
      }
      setState(() {
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

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    final token = _auth?.accessToken;
    if (token == null) return;

    final payload = VendorProfileModel(
      id: '',
      userId: _auth?.userId ?? '',
      businessName: _businessNameController.text.trim(),
      category: _category,
      contactEmail: _contactEmailController.text.trim(),
      contactPhone: _contactPhoneController.text.trim(),
      description: _descriptionController.text.trim().isEmpty
          ? null
          : _descriptionController.text.trim(),
      status: _status ?? '',
    );

    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      final saved = _hasProfile
          ? await _vendorApi.updateProfile(token, payload)
          : await _vendorApi.createProfile(token, payload);
      if (!mounted) return;
      setState(() {
        _saving = false;
        _hasProfile = true;
        _status = saved.status;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Vendor profile saved successfully.'),
          backgroundColor: AppColors.success,
        ),
      );
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _error = e.toString();
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        title:
            Text(_hasProfile ? 'Edit vendor profile' : 'Create vendor profile'),
      ),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(
                valueColor:
                    AlwaysStoppedAnimation<Color>(AppColors.obsidianBlack),
              ),
            )
          : SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(
                AppDimens.space20,
                AppDimens.space12,
                AppDimens.space20,
                AppDimens.space32,
              ),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Business Hero Card inspired by the reference top card
                    PastelCard(
                      padding: const EdgeInsets.all(AppDimens.space20),
                      child: Column(
                        children: [
                          Row(
                            children: [
                              Container(
                                width: 68,
                                height: 68,
                                decoration: BoxDecoration(
                                  color: AppColors.pastelGreenLight,
                                  borderRadius: BorderRadius.circular(20),
                                  border: Border.all(
                                    color: const Color(0x30C4DDB8),
                                    width: 1.5,
                                  ),
                                ),
                                child: const Icon(
                                  Icons.storefront_rounded,
                                  color: AppColors.pastelGreenText,
                                  size: 34,
                                ),
                              ),
                              const SizedBox(width: AppDimens.space16),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      _businessNameController.text.isNotEmpty
                                          ? _businessNameController.text
                                          : 'Your Business',
                                      style: const TextStyle(
                                        color: AppColors.textPrimary,
                                        fontSize: 20,
                                        fontWeight: FontWeight.w800,
                                        letterSpacing: -0.4,
                                      ),
                                    ),
                                    const SizedBox(height: 2),
                                    Text(
                                      _category,
                                      style: const TextStyle(
                                        color: AppColors.textSecondary,
                                        fontSize: 13,
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      _contactEmailController.text.isNotEmpty
                                          ? _contactEmailController.text
                                          : (_auth?.email ?? 'Verified vendor'),
                                      style: const TextStyle(
                                        color: AppColors.textMuted,
                                        fontSize: 12,
                                      ),
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: AppDimens.space16),
                          PastelRibbonBanner(
                            icon: Icons.storefront_outlined,
                            title: _hasProfile
                                ? 'REGISTERED VENDOR'
                                : 'NEW VENDOR PROFILE',
                            subtitle: _status != null && _status!.isNotEmpty
                                ? 'STATUS: $_status'
                                : 'PENDING ACTIVATION',
                          ),
                        ],
                      ),
                    ),

                    if (_error != null) ...[
                      const SizedBox(height: AppDimens.space16),
                      Container(
                        padding: const EdgeInsets.all(AppDimens.space14),
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
                    ],

                    const PastelSectionHeader(title: 'Business Information'),

                    PastelCard(
                      padding: const EdgeInsets.all(AppDimens.space20),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          CustomTextField(
                            label: 'Business name',
                            hint: 'Green Leaf Catering',
                            controller: _businessNameController,
                            prefixIcon: const Icon(
                              Icons.business_rounded,
                              color: AppColors.textSecondary,
                            ),
                            validator: (value) =>
                                value == null || value.trim().isEmpty
                                    ? 'Required'
                                    : null,
                          ),
                          const SizedBox(height: AppDimens.space16),
                          Text(
                            'Category',
                            style:
                                Theme.of(context).textTheme.titleSmall?.copyWith(
                                      fontWeight: FontWeight.w700,
                                    ),
                          ),
                          const SizedBox(height: 8),
                          DropdownButtonFormField<String>(
                            initialValue: _category,
                            decoration: const InputDecoration(
                              hintText: 'Select category',
                            ),
                            items: vendorCategories
                                .map((c) => DropdownMenuItem(
                                      value: c,
                                      child: Text(
                                        c,
                                        style: const TextStyle(
                                            fontWeight: FontWeight.w600),
                                      ),
                                    ))
                                .toList(),
                            onChanged: (newValue) {
                              if (newValue != null) {
                                setState(() => _category = newValue);
                              }
                            },
                          ),
                        ],
                      ),
                    ),

                    const PastelSectionHeader(title: 'Contact & Details'),

                    PastelCard(
                      padding: const EdgeInsets.all(AppDimens.space20),
                      child: Column(
                        children: [
                          CustomTextField(
                            label: 'Contact email',
                            hint: 'vendor@business.com',
                            controller: _contactEmailController,
                            keyboardType: TextInputType.emailAddress,
                            prefixIcon: const Icon(
                              Icons.email_outlined,
                              color: AppColors.textSecondary,
                            ),
                            validator: (value) {
                              if (value == null || value.trim().isEmpty) {
                                return 'Required';
                              }
                              if (!value.contains('@')) {
                                return 'Enter a valid email';
                              }
                              return null;
                            },
                          ),
                          const SizedBox(height: AppDimens.space16),
                          CustomTextField(
                            label: 'Contact phone',
                            hint: '0771234567',
                            controller: _contactPhoneController,
                            keyboardType: TextInputType.phone,
                            prefixIcon: const Icon(
                              Icons.phone_outlined,
                              color: AppColors.textSecondary,
                            ),
                            validator: (value) =>
                                value == null || value.trim().isEmpty
                                    ? 'Required'
                                    : null,
                          ),
                          const SizedBox(height: AppDimens.space16),
                          CustomTextField(
                            label: 'Description',
                            hint:
                                'Tell clients about your services, experience, and specialties...',
                            controller: _descriptionController,
                            maxLines: 3,
                          ),
                        ],
                      ),
                    ),

                    const SizedBox(height: AppDimens.space24),

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
                                  valueColor: AlwaysStoppedAnimation<Color>(
                                      Colors.white),
                                ),
                              )
                            : Text(_hasProfile
                                ? 'Save changes'
                                : 'Create profile'),
                      ),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}
