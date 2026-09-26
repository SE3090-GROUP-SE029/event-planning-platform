import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../api/plan_remote_datasource.dart';
import '../models/plan_model.dart';

final planApiProvider = Provider((ref) => PlanRemoteDataSource());
final planProvider = ChangeNotifierProvider.autoDispose
    .family<PlanController, ({String token, String planId})>((ref, args) {
  return PlanController(ref.read(planApiProvider), args.token, args.planId);
});

class PlanController extends ChangeNotifier {
  final PlanRemoteDataSource api;
  final String token;
  final String planId;
  EventPlan? plan;
  bool loading = false;
  bool submitting = false;
  String? error;
  bool fromCache = false;

  PlanController(this.api, this.token, this.planId);

  Future<void> load() async {
    if (loading) return;
    loading = true;
    error = null;
    notifyListeners();
    try {
      plan = await api.get(token, planId);
      fromCache = false;
      await _cache();
    } catch (exception) {
      final cached = await SharedPreferences.getInstance();
      final value = cached.getString('plan_$planId');
      if (value != null) {
        plan = EventPlan.fromJson(jsonDecode(value) as Map<String, dynamic>);
        fromCache = true;
      } else {
        error = 'Unable to load this plan. Check your connection and retry.';
      }
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<bool> approve(String? notes) => _submit(
        () => api.approve(token, planId, notes),
      );

  Future<bool> reject(String remarks, RejectionSeverity severity) => _submit(
        () => api.reject(token, planId, remarks, severity),
      );

  Future<bool> regenerate() => _submit(
        () => api.regenerate(token, plan?.eventId ?? ''),
      );

  Future<bool> _submit(Future<EventPlan> Function() action) async {
    if (submitting) return false;
    submitting = true;
    error = null;
    notifyListeners();
    try {
      plan = await action();
      fromCache = false;
      await _cache();
      return true;
    } catch (_) {
      error = 'The request failed. Please try again.';
      return false;
    } finally {
      submitting = false;
      notifyListeners();
    }
  }

  Future<void> _cache() async {
    final current = plan;
    if (current == null) return;
    final preferences = await SharedPreferences.getInstance();
    await preferences.setString('plan_$planId', jsonEncode(current.toJson()));
  }
}
