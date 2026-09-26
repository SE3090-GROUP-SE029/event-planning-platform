import 'package:dio/dio.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import '../../../core/api/dio_client.dart';
import '../models/marketplace_vendor_model.dart';
import '../models/vendor_availability_model.dart';
import '../models/vendor_gallery_image_model.dart';
import '../models/vendor_profile_model.dart';
import '../models/vendor_service_model.dart';

class VendorRemoteDataSource {
  final Dio dio;

  VendorRemoteDataSource({Dio? dio}) : dio = dio ?? DioClient().dio;

  static String? resolveImageUrl(String? profileImageUrl) {
    if (profileImageUrl == null || profileImageUrl.isEmpty) return null;
    if (profileImageUrl.startsWith('http://') ||
        profileImageUrl.startsWith('https://')) {
      return profileImageUrl;
    }
    final base = dotenv.env['API_BASE_URL'] ?? '';
    if (base.isEmpty) return profileImageUrl;
    final normalizedBase = base.endsWith('/')
        ? base.substring(0, base.length - 1)
        : base;
    final path =
        profileImageUrl.startsWith('/') ? profileImageUrl : '/$profileImageUrl';
    return '$normalizedBase$path';
  }

  Options _auth(String accessToken) => Options(
        headers: {'Authorization': 'Bearer $accessToken'},
      );

  Future<VendorProfileModel?> getMyProfile(String accessToken) async {
    try {
      final response = await dio.get(
        '/api/vendors/me',
        options: _auth(accessToken),
      );
      return VendorProfileModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) {
        return null;
      }
      throw _handleError(e);
    }
  }

  Future<VendorProfileModel> createProfile(
    String accessToken,
    VendorProfileModel profile,
  ) async {
    try {
      final response = await dio.post(
        '/api/vendors',
        data: profile.toJson(),
        options: _auth(accessToken),
      );
      return VendorProfileModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<VendorProfileModel> updateProfile(
    String accessToken,
    VendorProfileModel profile,
  ) async {
    try {
      final response = await dio.put(
        '/api/vendors/me',
        data: profile.toJson(),
        options: _auth(accessToken),
      );
      return VendorProfileModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<VendorProfileModel> uploadProfileImage(
    String accessToken,
    String filePath,
    String fileName,
  ) async {
    try {
      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(filePath, filename: fileName),
      });
      final response = await dio.post(
        '/api/vendors/me/profile-image',
        data: formData,
        options: Options(
          headers: {
            'Authorization': 'Bearer $accessToken',
            'Content-Type': 'multipart/form-data',
          },
        ),
      );
      return VendorProfileModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<List<VendorGalleryImageModel>> listGalleryImages(
    String accessToken,
  ) async {
    try {
      final response = await dio.get(
        '/api/vendors/me/images',
        options: _auth(accessToken),
      );
      final data = response.data as List<dynamic>;
      return data
          .map((item) =>
              VendorGalleryImageModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<VendorGalleryImageModel> uploadGalleryImage(
    String accessToken,
    String filePath,
    String fileName,
  ) async {
    try {
      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(filePath, filename: fileName),
      });
      final response = await dio.post(
        '/api/vendors/me/images',
        data: formData,
        options: Options(
          headers: {
            'Authorization': 'Bearer $accessToken',
            'Content-Type': 'multipart/form-data',
          },
        ),
      );
      return VendorGalleryImageModel.fromJson(
        response.data as Map<String, dynamic>,
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<void> deleteGalleryImage(String accessToken, String imageId) async {
    try {
      await dio.delete(
        '/api/vendors/me/images/$imageId',
        options: _auth(accessToken),
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<List<VendorServiceModel>> listMyServices(String accessToken) async {
    try {
      final response = await dio.get(
        '/api/vendors/me/services',
        options: _auth(accessToken),
      );
      final data = response.data as List<dynamic>;
      return data
          .map((item) =>
              VendorServiceModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<VendorServiceModel> createService(
    String accessToken,
    VendorServiceModel service,
  ) async {
    try {
      final response = await dio.post(
        '/api/vendors/me/services',
        data: service.toJson(),
        options: _auth(accessToken),
      );
      return VendorServiceModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<VendorServiceModel> updateService(
    String accessToken,
    String serviceId,
    VendorServiceModel service,
  ) async {
    try {
      final response = await dio.put(
        '/api/vendors/me/services/$serviceId',
        data: service.toJson(),
        options: _auth(accessToken),
      );
      return VendorServiceModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<void> deleteService(String accessToken, String serviceId) async {
    try {
      await dio.delete(
        '/api/vendors/me/services/$serviceId',
        options: _auth(accessToken),
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<List<VendorAvailabilityModel>> listMyAvailability(
    String accessToken,
  ) async {
    try {
      final response = await dio.get(
        '/api/vendors/me/availability',
        options: _auth(accessToken),
      );
      final data = response.data as List<dynamic>;
      return data
          .map((item) =>
              VendorAvailabilityModel.fromJson(item as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<VendorAvailabilityModel> createAvailability(
    String accessToken,
    VendorAvailabilityModel availability,
  ) async {
    try {
      final response = await dio.post(
        '/api/vendors/me/availability',
        data: availability.toJson(),
        options: _auth(accessToken),
      );
      return VendorAvailabilityModel.fromJson(
        response.data as Map<String, dynamic>,
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<VendorAvailabilityModel> updateAvailability(
    String accessToken,
    String availabilityId,
    VendorAvailabilityModel availability,
  ) async {
    try {
      final response = await dio.put(
        '/api/vendors/me/availability/$availabilityId',
        data: availability.toJson(),
        options: _auth(accessToken),
      );
      return VendorAvailabilityModel.fromJson(
        response.data as Map<String, dynamic>,
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<void> deleteAvailability(
    String accessToken,
    String availabilityId,
  ) async {
    try {
      await dio.delete(
        '/api/vendors/me/availability/$availabilityId',
        options: _auth(accessToken),
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<MarketplaceVendorListResult> listMarketplace(
    String accessToken, {
    String? search,
    String? category,
    String sortBy = 'businessName',
    String sortOrder = 'asc',
    int page = 1,
    int pageSize = 10,
  }) async {
    try {
      final response = await dio.get(
        '/api/vendors/marketplace',
        queryParameters: {
          if (search != null && search.isNotEmpty) 'search': search,
          if (category != null && category.isNotEmpty) 'category': category,
          'sortBy': sortBy,
          'sortOrder': sortOrder,
          'page': page,
          'pageSize': pageSize,
        },
        options: _auth(accessToken),
      );
      return MarketplaceVendorListResult.fromJson(
        response.data as Map<String, dynamic>,
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  Future<MarketplaceVendorDetail> getMarketplaceVendor(
    String accessToken,
    String vendorId,
  ) async {
    try {
      final response = await dio.get(
        '/api/vendors/marketplace/$vendorId',
        options: _auth(accessToken),
      );
      return MarketplaceVendorDetail.fromJson(
        response.data as Map<String, dynamic>,
      );
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  String _handleError(DioException error) {
    if (error.response?.data is Map<String, dynamic>) {
      final data = error.response!.data as Map<String, dynamic>;
      if (data.containsKey('message')) return data['message'].toString();
    }
    return error.message ?? 'Unable to complete vendor request.';
  }
}
