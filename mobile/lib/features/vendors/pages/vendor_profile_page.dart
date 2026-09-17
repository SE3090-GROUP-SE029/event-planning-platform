import 'package:flutter/material.dart';
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
          content: Text('Vendor profile saved.'),
          backgroundColor: Colors.green,
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
      appBar: AppBar(title: Text(_hasProfile ? 'Edit Vendor Profile' : 'Create Vendor Profile')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    if (_status != null)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: Chip(label: Text('Status: $_status')),
                      ),
                    if (_error != null)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: Text(_error!, style: const TextStyle(color: Colors.red)),
                      ),
                    CustomTextField(
                      label: 'Business name',
                      hint: 'Green Leaf Catering',
                      controller: _businessNameController,
                      validator: (value) =>
                          value == null || value.trim().isEmpty ? 'Required' : null,
                    ),
                    const SizedBox(height: 16),
                    const Text('Category'),
                    const SizedBox(height: 8),
                    DropdownButtonFormField<String>(
                      initialValue: _category,
                      items: vendorCategories
                          .map((c) => DropdownMenuItem(value: c, child: Text(c)))
                          .toList(),
                      onChanged: (newValue) {
                        if (newValue != null) {
                          setState(() => _category = newValue);
                        }
                      },
                    ),
                    const SizedBox(height: 16),
                    CustomTextField(
                      label: 'Contact email',
                      hint: 'vendor@business.com',
                      controller: _contactEmailController,
                      keyboardType: TextInputType.emailAddress,
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) return 'Required';
                        if (!value.contains('@')) return 'Enter a valid email';
                        return null;
                      },
                    ),
                    const SizedBox(height: 16),
                    CustomTextField(
                      label: 'Contact phone',
                      hint: '0771234567',
                      controller: _contactPhoneController,
                      keyboardType: TextInputType.phone,
                      validator: (value) =>
                          value == null || value.trim().isEmpty ? 'Required' : null,
                    ),
                    const SizedBox(height: 16),
                    CustomTextField(
                      label: 'Description',
                      hint: 'Short description of your services',
                      controller: _descriptionController,
                      maxLines: 3,
                    ),
                    const SizedBox(height: 24),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton(
                        onPressed: _saving ? null : _save,
                        child: _saving
                            ? const SizedBox(
                                height: 20,
                                width: 20,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              )
                            : Text(_hasProfile ? 'Save changes' : 'Create profile'),
                      ),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}
