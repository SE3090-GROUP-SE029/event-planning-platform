import { useState } from 'react';
import { testApi } from './api/testApi'; 

export function TestApiWidget() {
  const [pingResult, setPingResult] = useState(null);
  const [messageInput, setMessageInput] = useState('');
  const [messages, setMessages] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const handlePing = async () => {
    try {
      setLoading(true);
      setError(null);

      const result = await testApi.ping();

      setPingResult(result);
      console.log('✅ Ping successful:', result);
    } catch (err) {
      setError(`Ping failed: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('❌ Ping failed:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateMessage = async () => {
    if (!messageInput.trim()) {
      setError('Message cannot be empty');
      return;
    }

    try {
      setLoading(true);
      setError(null);

      const result = await testApi.createMessage(messageInput);

      setMessages([...messages, result]);
      setMessageInput('');
      console.log('✅ Message created:', result);
    } catch (err) {
      setError(`Create failed: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('❌ Create failed:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        maxWidth: '600px',
        margin: '0 auto',
        padding: '20px',
        fontFamily: 'sans-serif',
      }}
    >
      <h1>🧪 Backend Integration Test</h1>

      {error && (
        <div
          style={{
            padding: '10px',
            backgroundColor: '#fee',
            color: '#c00',
            borderRadius: '4px',
            marginBottom: '10px',
          }}
        >
          {error}
        </div>
      )}

      {/* Ping Section */}
      <section
        style={{
          marginBottom: '20px',
          padding: '10px',
          border: '1px solid #ddd',
          borderRadius: '4px',
        }}
      >
        <h2>1. Ping Test</h2>

        <button onClick={handlePing} disabled={loading}>
          {loading ? 'Pinging...' : 'Send Ping'}
        </button>

        {pingResult && (
          <pre
            style={{
              backgroundColor: '#f5f5f5',
              padding: '10px',
              borderRadius: '4px',
              marginTop: '10px',
            }}
          >
            {JSON.stringify(pingResult, null, 2)}
          </pre>
        )}
      </section>

      {/* Message Creation Section */}
      <section
        style={{
          marginBottom: '20px',
          padding: '10px',
          border: '1px solid #ddd',
          borderRadius: '4px',
        }}
      >
        <h2>2. Create Message</h2>

        <div style={{ marginBottom: '10px' }}>
          <input
            type="text"
            placeholder="Enter a test message..."
            value={messageInput}
            onChange={(e) => setMessageInput(e.target.value)}
            onKeyPress={(e) =>
              e.key === 'Enter' && handleCreateMessage()
            }
            style={{
              width: '100%',
              padding: '8px',
              marginBottom: '8px',
              boxSizing: 'border-box',
            }}
          />

          <button onClick={handleCreateMessage} disabled={loading}>
            {loading ? 'Creating...' : 'Create Message'}
          </button>
        </div>

        {messages.length > 0 && (
          <div style={{ marginTop: '10px' }}>
            <h3>Created Messages:</h3>

            <ul style={{ paddingLeft: '20px' }}>
              {messages.map((msg) => (
                <li key={msg.id}>
                  <strong>ID {msg.id}:</strong> {msg.message}{' '}
                  <em>
                    ({new Date(msg.createdAt).toLocaleString()})
                  </em>
                </li>
              ))}
            </ul>
          </div>
        )}
      </section>

      {/* Status Section */}
      <section
        style={{
          padding: '10px',
          border: '1px solid #ddd',
          borderRadius: '4px',
          backgroundColor: '#f9f9f9',
        }}
      >
        <h3>Status</h3>

        <p>
          API Base URL:{' '}
          <code>
            {import.meta.env.VITE_API_BASE_URL ||
              'http://localhost:5000'}
          </code>
        </p>

        <p>
          Backend Connection:{' '}
          {pingResult ? '✅ Connected' : '⏳ Not tested yet'}
        </p>
      </section>
    </div>
  );
}