import 'package:flutter/material.dart';
import '../models/test_models.dart';
import '../services/test_api.dart';

class TestScreen extends StatefulWidget {
  const TestScreen({super.key});

  @override
  State<TestScreen> createState() => _TestScreenState();
}

class _TestScreenState extends State<TestScreen> {
  final _testApi = TestApi();
  final _messageController = TextEditingController();
  
  PingResponse? _pingResult;
  final List<TestMessage> _messages = [];
  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _messageController.dispose();
    super.dispose();
  }

  Future<void> _handlePing() async {
    setState(() => _loading = true);
    try {
      final result = await _testApi.ping();
      setState(() {
        _pingResult = result;
        _error = null;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('✅ Ping successful')),
      );
    } catch (e) {
      setState(() => _error = 'Ping failed: $e');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('❌ ${e.toString()}')),
      );
    } finally {
      setState(() => _loading = false);
    }
  }

  Future<void> _handleCreateMessage() async {
    if (_messageController.text.isEmpty) {
      setState(() => _error = 'Message cannot be empty');
      return;
    }

    setState(() => _loading = true);
    try {
      final result = await _testApi.createMessage(_messageController.text);
      setState(() {
        _messages.add(result);
        _messageController.clear();
        _error = null;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('✅ Message created')),
      );
    } catch (e) {
      setState(() => _error = 'Create failed: $e');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('❌ ${e.toString()}')),
      );
    } finally {
      setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('🧪 Backend Integration Test'),
        backgroundColor: Colors.blue,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (_error != null)
              Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(
                  color: Colors.red.shade100,
                  border: Border.all(color: Colors.red),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  _error!,
                  style: TextStyle(color: Colors.red.shade900),
                ),
              ),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('1. Ping Test', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 12),
                    ElevatedButton(
                      onPressed: _loading ? null : _handlePing,
                      child: Text(_loading ? 'Pinging...' : 'Send Ping'),
                    ),
                    if (_pingResult != null) ...[
                      const SizedBox(height: 12),
                      Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: Colors.grey.shade100,
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: Text(
                          'Response: ${_pingResult!.message}\nTime: ${_pingResult!.timestamp}',
                          style: const TextStyle(fontFamily: 'monospace', fontSize: 12),
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('2. Create Message', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _messageController,
                      decoration: InputDecoration(
                        hintText: 'Enter a test message...',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(4)),
                        contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      ),
                      onSubmitted: (_) => _handleCreateMessage(),
                    ),
                    const SizedBox(height: 8),
                    ElevatedButton(
                      onPressed: _loading ? null : _handleCreateMessage,
                      child: Text(_loading ? 'Creating...' : 'Create Message'),
                    ),
                    if (_messages.isNotEmpty) ...[
                      const SizedBox(height: 12),
                      const Text('Created Messages:', style: TextStyle(fontWeight: FontWeight.bold)),
                      const SizedBox(height: 8),
                      ..._messages.map((msg) => Padding(
                        padding: const EdgeInsets.symmetric(vertical: 4),
                        child: Text(
                          'ID ${msg.id}: ${msg.message}',
                          style: const TextStyle(fontFamily: 'monospace', fontSize: 12),
                        ),
                      )),
                    ],
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Status', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 8),
                    Text('Backend Connection: ${_pingResult != null ? '✅ Connected' : '⏳ Not tested yet'}'),
                    const SizedBox(height: 4),
                    const Text('API Base: http://localhost:5000', style: TextStyle(fontFamily: 'monospace', fontSize: 12)),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
