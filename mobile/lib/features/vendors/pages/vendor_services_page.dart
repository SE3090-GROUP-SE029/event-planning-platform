import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../shared/widgets/pastel_card.dart';
import '../../../shared/widgets/pastel_section_header.dart';
import '../../auth/models/auth_response_model.dart';
import '../../auth/widgets/custom_text_field.dart';
import '../api/vendor_remote_datasource.dart';
import '../models/vendor_service_model.dart';

const _pricingTypes = [
  ('FIXED', 'Fixed (LKR)'),
  ('PER_PERSON', 'Per person (LKR)'),
  ('PER_HOUR', 'Per hour (LKR)'),
  ('PER_DAY', 'Per day (LKR)'),
];

class VendorServicesPage extends StatefulWidget {
  const VendorServicesPage({super.key});

  @override
  State<VendorServicesPage> createState() => _VendorServicesPageState();
}

class _VendorServicesPageState extends State<VendorServicesPage> {
  final _formKey = GlobalKey<FormState>();
  final _serviceNameController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _priceController = TextEditingController();
  final _vendorApi = VendorRemoteDataSource();

  List<VendorServiceModel> _services = [];
  String? _editingId;
  String? _pricingType;
  bool _loading = true;
  bool _saving = false;
  String? _error;

  AuthResponseModel? get _auth =>
      ModalRoute.of(context)?.settings.arguments as AuthResponseModel?;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_loading) {
      _loadServices();
    }
  }

  @override
  void dispose() {
    _serviceNameController.dispose();
    _descriptionController.dispose();
    _priceController.dispose();
    super.dispose();
  }

  Future<void> _loadServices() async {
    final token = _auth?.accessToken;
    if (token == null || token.isEmpty) {
      setState(() {
        _loading = false;
        _error = 'Please log in as a vendor first.';
      });
      return;
    }

    try {
      final services = await _vendorApi.listMyServices(token);
      if (!mounted) return;
      setState(() {
        _services = services;
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

  void _startEdit(VendorServiceModel service) {
    setState(() {
      _editingId = service.id;
      _serviceNameController.text = service.serviceName;
      _descriptionController.text = service.description ?? '';
      _priceController.text =
          service.price == null ? '' : service.price!.toStringAsFixed(2);
      _pricingType = service.pricingType;
    });
  }

  void _clearForm() {
    setState(() {
      _editingId = null;
      _serviceNameController.clear();
      _descriptionController.clear();
      _priceController.clear();
      _pricingType = null;
    });
  }

  void _clearPriceFields() {
    setState(() {
      _priceController.clear();
      _pricingType = null;
    });
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    final token = _auth?.accessToken;
    if (token == null) return;

    final rawPrice = _priceController.text.trim();
    double? price;
    String? pricingType;

    if (rawPrice.isNotEmpty) {
      price = double.tryParse(rawPrice);
      if (price == null || price < 0) {
        setState(() => _error = 'Enter a valid price of Rs. 0 or more');
        return;
      }
      if (_pricingType == null || _pricingType!.isEmpty) {
        setState(() => _error = 'Select a pricing type when setting a price');
        return;
      }
      pricingType = _pricingType;
    }

    final payload = VendorServiceModel(
      id: _editingId ?? '',
      vendorId: '',
      serviceName: _serviceNameController.text.trim(),
      description: _descriptionController.text.trim().isEmpty
          ? null
          : _descriptionController.text.trim(),
      price: price,
      pricingType: pricingType,
    );

    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      if (_editingId == null) {
        await _vendorApi.createService(token, payload);
      } else {
        await _vendorApi.updateService(token, _editingId!, payload);
      }

      if (!mounted) return;
      _clearForm();

      final services = await _vendorApi.listMyServices(token);

      // Required because the previous listMyServices call is another async gap.
      if (!mounted) return;

      setState(() {
        _services = services;
        _saving = false;
      });

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Service saved.'),
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

  Future<void> _delete(String serviceId) async {
    final token = _auth?.accessToken;
    if (token == null) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete service?'),
        content: const Text(
          'This removes the service from your vendor catalog.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;
    if (!mounted) return;

    try {
      await _vendorApi.deleteService(token, serviceId);
      if (!mounted) return;
      final services = await _vendorApi.listMyServices(token);
      if (!mounted) return;
      setState(() {
        _services = services;
        if (_editingId == serviceId) {
          _clearForm();
        }
      });
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
        title: const Text('Vendor services'),
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
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
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
                  PastelSectionHeader(
                    title: _editingId == null ? 'Add service' : 'Edit service',
                  ),
                  PastelCard(
                    padding: const EdgeInsets.all(AppDimens.space20),
                    child: Form(
                      key: _formKey,
                      child: Column(
                        children: [
                          CustomTextField(
                            label: 'Service name',
                            hint: 'Wedding Photography',
                            controller: _serviceNameController,
                            validator: (value) =>
                                value == null || value.trim().isEmpty
                                    ? 'Required'
                                    : null,
                          ),
                          const SizedBox(height: AppDimens.space16),
                          CustomTextField(
                            label: 'Description',
                            hint: 'Full-day wedding photography service',
                            controller: _descriptionController,
                            maxLines: 3,
                          ),
                          const SizedBox(height: AppDimens.space16),
                          CustomTextField(
                            label: 'Price (Rs. / LKR)',
                            hint: '85000.00',
                            controller: _priceController,
                            keyboardType: const TextInputType.numberWithOptions(
                              decimal: true,
                            ),
                          ),
                          const SizedBox(height: AppDimens.space16),
                          DropdownButtonFormField<String>(
                            key: ValueKey(_pricingType ?? ''),
                            initialValue: _pricingType ?? '',
                            decoration: const InputDecoration(
                              labelText: 'Pricing type',
                              border: OutlineInputBorder(),
                            ),
                            items: [
                              const DropdownMenuItem<String>(
                                value: '',
                                child: Text('None'),
                              ),
                              ..._pricingTypes.map(
                                (option) => DropdownMenuItem<String>(
                                  value: option.$1,
                                  child: Text(option.$2),
                                ),
                              ),
                            ],
                            onChanged: (value) {
                              setState(() {
                                _pricingType =
                                    (value == null || value.isEmpty) ? null : value;
                              });
                            },
                          ),
                          const SizedBox(height: AppDimens.space16),
                          SizedBox(
                            width: double.infinity,
                            height: 48,
                            child: ElevatedButton(
                              onPressed: _saving ? null : _save,
                              child: Text(_editingId == null
                                  ? 'Add service'
                                  : 'Save changes'),
                            ),
                          ),
                          if (_editingId != null) ...[
                            const SizedBox(height: AppDimens.space8),
                            TextButton(
                              onPressed: _clearForm,
                              child: const Text('Cancel edit'),
                            ),
                            TextButton(
                              onPressed: _clearPriceFields,
                              child: const Text('Clear price'),
                            ),
                          ],
                        ],
                      ),
                    ),
                  ),
                  const PastelSectionHeader(title: 'Your services'),
                  if (_services.isEmpty)
                    const PastelCard(
                      padding: EdgeInsets.all(AppDimens.space20),
                      child: Text('No services yet. Add your first service.'),
                    )
                  else
                    ..._services.map(
                      (service) => Padding(
                        padding:
                            const EdgeInsets.only(bottom: AppDimens.space12),
                        child: PastelCard(
                          padding: const EdgeInsets.all(AppDimens.space16),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      service.serviceName,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w800,
                                        fontSize: 16,
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
                              IconButton(
                                onPressed: () => _startEdit(service),
                                icon: const Icon(Icons.edit_outlined),
                              ),
                              IconButton(
                                onPressed: () => _delete(service.id),
                                icon: const Icon(Icons.delete_outline),
                                color: AppColors.error,
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
