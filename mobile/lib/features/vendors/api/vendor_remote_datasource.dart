import 'package:dio/dio.dart';
import '../../../core/api/dio_client.dart';
import '../models/vendor_profile_model.dart';

class VendorRemoteDataSource {
  final Dio dio;

  VendorRemoteDataSource({Dio? dio}) : dio = dio ?? DioClient().dio;

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

  String _handleError(DioException error) {
    if (error.response?.data is Map<String, dynamic>) {
      final data = error.response!.data as Map<String, dynamic>;
      if (data.containsKey('message')) return data['message'].toString();
    }
    return error.message ?? 'Unable to save vendor profile.';
  }
}
