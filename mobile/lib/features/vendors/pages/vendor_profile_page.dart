import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_pill_badge.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../../auth/widgets/custom_text_field.dart';
import '../api/vendor_remote_datasource.dart';
import '../models/vendor_gallery_image_model.dart';
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
  final _addressController = TextEditingController();
  final _websiteController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _vendorApi = VendorRemoteDataSource();
  final _imagePicker = ImagePicker();

  String _category = 'CATERING';
  String? _profileImageUrl;
  List<VendorGalleryImageModel> _gallery = [];
  bool _loading = true;
  bool _saving = false;
  bool _uploadingImage = false;
  bool _uploadingGallery = false;
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
    _addressController.dispose();
    _websiteController.dispose();
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
        _addressController.text = profile.address;
        _websiteController.text = profile.websiteUrl ?? '';
        _descriptionController.text = profile.description ?? '';
        _profileImageUrl = profile.profileImageUrl;
        _category = vendorCategories.contains(profile.category)
            ? profile.category
            : 'CATERING';
        _status = profile.status;
        _hasProfile = true;
        try {
          _gallery = await _vendorApi.listGalleryImages(token);
        } catch (_) {
          _gallery = [];
        }
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
      address: _addressController.text.trim(),
      description: _descriptionController.text.trim().isEmpty
          ? null
          : _descriptionController.text.trim(),
      profileImageUrl: _profileImageUrl,
      websiteUrl: _websiteController.text.trim().isEmpty
          ? null
          : _websiteController.text.trim(),
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
        _profileImageUrl = saved.profileImageUrl;
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

  Future<void> _pickAndUploadImage() async {
    final token = _auth?.accessToken;
    if (token == null || !_hasProfile) return;

    final picked = await _imagePicker.pickImage(
      source: ImageSource.gallery,
      maxWidth: 1200,
      imageQuality: 85,
    );
    if (picked == null || !mounted) return;

    setState(() {
      _uploadingImage = true;
      _error = null;
    });

    try {
      final saved = await _vendorApi.uploadProfileImage(
        token,
        picked.path,
        picked.name,
      );
      if (!mounted) return;
      setState(() {
        _uploadingImage = false;
        _profileImageUrl = saved.profileImageUrl;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Profile image updated.'),
          backgroundColor: AppColors.success,
        ),
      );
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _uploadingImage = false;
        _error = e.toString();
      });
    }
  }

  Future<void> _pickAndUploadGalleryImage() async {
    final token = _auth?.accessToken;
    if (token == null || !_hasProfile) return;

    final picked = await _imagePicker.pickImage(
      source: ImageSource.gallery,
      maxWidth: 1600,
      imageQuality: 85,
    );
    if (picked == null || !mounted) return;

    setState(() {
      _uploadingGallery = true;
      _error = null;
    });

    try {
      await _vendorApi.uploadGalleryImage(token, picked.path, picked.name);
      if (!mounted) return;
      final gallery = await _vendorApi.listGalleryImages(token);
      if (!mounted) return;
      setState(() {
        _gallery = gallery;
        _uploadingGallery = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _uploadingGallery = false;
        _error = e.toString();
      });
    }
  }

  Future<void> _deleteGalleryImage(String imageId) async {
    final token = _auth?.accessToken;
    if (token == null) return;
    try {
      await _vendorApi.deleteGalleryImage(token, imageId);
      if (!mounted) return;
      final gallery = await _vendorApi.listGalleryImages(token);
      if (!mounted) return;
      setState(() => _gallery = gallery);
    } catch (e) {
      if (!mounted) return;
      setState(() => _error = e.toString());
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
                              GestureDetector(
                                onTap: _hasProfile && !_uploadingImage
                                    ? _pickAndUploadImage
                                    : null,
                                child: CircleAvatar(
                                  radius: 34,
                                  backgroundColor: AppColors.pastelGreenLight,
                                  backgroundImage: VendorRemoteDataSource
                                              .resolveImageUrl(
                                                  _profileImageUrl) !=
                                          null
                                      ? NetworkImage(
                                          VendorRemoteDataSource
                                              .resolveImageUrl(
                                                  _profileImageUrl)!,
                                        )
                                      : null,
                                  child: _uploadingImage
                                      ? const SizedBox(
                                          width: 22,
                                          height: 22,
                                          child: CircularProgressIndicator(
                                            strokeWidth: 2,
                                          ),
                                        )
                                      : (VendorRemoteDataSource.resolveImageUrl(
                                                  _profileImageUrl) ==
                                              null
                                          ? const Icon(
                                              Icons.storefront_rounded,
                                              color: AppColors.pastelGreenText,
                                              size: 34,
                                            )
                                          : null),
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
                                      _hasProfile
                                          ? 'Tap logo to upload/change image'
                                          : 'Save profile to upload a logo',
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
                            label: 'Address',
                            hint: '12 Flower Road, Colombo',
                            controller: _addressController,
                            prefixIcon: const Icon(
                              Icons.location_on_outlined,
                              color: AppColors.textSecondary,
                            ),
                            validator: (value) =>
                                value == null || value.trim().isEmpty
                                    ? 'Required'
                                    : null,
                          ),
                          const SizedBox(height: AppDimens.space16),
                          CustomTextField(
                            label: 'Website / social link',
                            hint: 'https://yourbusiness.com',
                            controller: _websiteController,
                            keyboardType: TextInputType.url,
                            prefixIcon: const Icon(
                              Icons.link_rounded,
                              color: AppColors.textSecondary,
                            ),
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

                    if (_hasProfile) ...[
                      const PastelSectionHeader(title: 'Business images'),
                      PastelCard(
                        padding: const EdgeInsets.all(AppDimens.space16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            SizedBox(
                              width: double.infinity,
                              child: OutlinedButton.icon(
                                onPressed: _uploadingGallery ||
                                        _gallery.length >= 8
                                    ? null
                                    : _pickAndUploadGalleryImage,
                                icon: _uploadingGallery
                                    ? const SizedBox(
                                        width: 16,
                                        height: 16,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                        ),
                                      )
                                    : const Icon(Icons.add_photo_alternate_outlined),
                                label: Text(_uploadingGallery
                                    ? 'Uploading…'
                                    : 'Add image'),
                              ),
                            ),
                            const SizedBox(height: AppDimens.space12),
                            if (_gallery.isEmpty)
                              const Text(
                                'No business images yet.',
                                style: TextStyle(color: AppColors.textSecondary),
                              )
                            else
                              Wrap(
                                spacing: 10,
                                runSpacing: 10,
                                children: _gallery.map((image) {
                                  final url =
                                      VendorRemoteDataSource.resolveImageUrl(
                                          image.imageUrl);
                                  return Stack(
                                    children: [
                                      ClipRRect(
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
                                              ),
                                      ),
                                      Positioned(
                                        top: 2,
                                        right: 2,
                                        child: InkWell(
                                          onTap: () =>
                                              _deleteGalleryImage(image.id),
                                          child: Container(
                                            decoration: const BoxDecoration(
                                              color: Colors.white,
                                              shape: BoxShape.circle,
                                            ),
                                            padding: const EdgeInsets.all(2),
                                            child: const Icon(
                                              Icons.close,
                                              size: 16,
                                              color: AppColors.error,
                                            ),
                                          ),
                                        ),
                                      ),
                                    ],
                                  );
                                }).toList(),
                              ),
                          ],
                        ),
                      ),
                      const SizedBox(height: AppDimens.space12),
                      SizedBox(
                        width: double.infinity,
                        height: 52,
                        child: OutlinedButton(
                          onPressed: () {
                            Navigator.of(context).pushNamed(
                              '/vendors/services',
                              arguments: _auth,
                            );
                          },
                          child: const Text('Manage services'),
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),
    );
  }
}
