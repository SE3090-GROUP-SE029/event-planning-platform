import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Box, Button, Stack, Typography, TextField, CircularProgress, Alert } from '@mui/material';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner';
import { Html5QrcodeScanner } from 'html5-qrcode';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import { useAdminEvent } from '../api/adminEventApi';
import { useMutation } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';

export default function GuestCheckInPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const eventQuery = useAdminEvent(id);

  const [tokenInput, setTokenInput] = useState('');
  const [scanResult, setScanResult] = useState(null);
  const [scanError, setScanError] = useState(null);

  const checkInMutation = useMutation({
    mutationFn: async (token) => {
      const res = await apiClient.post(`/api/events/${id}/registration-form/check-in`, { token });
      return res.data;
    },
    onSuccess: (data) => {
      setScanResult({ success: true, guest: data });
      setScanError(null);
    },
    onError: (error) => {
      setScanResult(null);
      setScanError(error.response?.data?.message || 'Check-in failed');
    }
  });

  useEffect(() => {
    const scanner = new Html5QrcodeScanner("reader", { fps: 10, qrbox: { width: 250, height: 250 } }, false);
    scanner.render((decodedText) => {
      if (!checkInMutation.isPending) {
        checkInMutation.mutate(decodedText);
      }
    }, () => {});

    return () => {
      scanner.clear().catch(e => console.error(e));
    };
  }, []);

  const handleManualCheckIn = (e) => {
    e.preventDefault();
    if (tokenInput.trim() && !checkInMutation.isPending) {
      checkInMutation.mutate(tokenInput.trim());
      setTokenInput('');
    }
  };

  return (
    <AppLayout
      activeTab="events"
      title="QR Check-In"
      subtitle={eventQuery.data ? `Scan guests for ${eventQuery.data.eventName}` : 'Event Check-In'}
    >
      <Stack direction="row" gap={2} sx={{ mb: 3 }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate(`/admin/events/${id}`)}
          sx={{ borderRadius: 9999, px: 2.5 }}
        >
          Back to Event Details
        </Button>
      </Stack>

      <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
        <SurfaceCard sx={{ p: 3, flex: 1, minWidth: 300, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          <Typography variant="h6" sx={{ mb: 2, fontWeight: 700 }}>Scan QR Code</Typography>
          <Box id="reader" sx={{ width: '100%', maxWidth: 400, mb: 3 }}></Box>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>Or enter token manually:</Typography>
          <form onSubmit={handleManualCheckIn} style={{ display: 'flex', gap: '8px', width: '100%', maxWidth: 400 }}>
            <TextField
              size="small"
              fullWidth
              placeholder="Invitation Token"
              value={tokenInput}
              onChange={(e) => setTokenInput(e.target.value)}
            />
            <Button type="submit" variant="contained" disabled={checkInMutation.isPending || !tokenInput.trim()}>
              Verify
            </Button>
          </form>
        </SurfaceCard>

        <SurfaceCard sx={{ p: 3, flex: 1, minWidth: 300, display: 'flex', flexDirection: 'column' }}>
          <Typography variant="h6" sx={{ mb: 3, fontWeight: 700 }}>Check-In Result</Typography>
          
          {checkInMutation.isPending && (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress />
            </Box>
          )}

          {!checkInMutation.isPending && !scanResult && !scanError && (
            <Box sx={{ textAlign: 'center', py: 4, color: 'text.secondary' }}>
              <QrCodeScannerIcon sx={{ fontSize: 64, opacity: 0.5, mb: 2 }} />
              <Typography>Scan a guest's QR code or enter their token to check them in.</Typography>
            </Box>
          )}

          {scanError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>Check-in Failed</Typography>
              {scanError}
            </Alert>
          )}

          {scanResult && (
            <Alert severity="success" sx={{ mb: 2 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>Check-in Successful!</Typography>
              Guest successfully checked into the event.
            </Alert>
          )}

          {scanResult?.guest && (
            <Box sx={{ mt: 2, p: 2, bgcolor: 'background.default', borderRadius: 1 }}>
              <Typography variant="subtitle2" color="text.secondary">Guest Details</Typography>
              <Typography sx={{ fontWeight: 700, mt: 1 }}>{scanResult.guest.guest?.fullName || scanResult.guest.fullName}</Typography>
              <Typography variant="body2">{scanResult.guest.guest?.emailAddress || scanResult.guest.emailAddress}</Typography>
              {scanResult.guest.guest?.organisation && <Typography variant="body2">{scanResult.guest.guest?.organisation}</Typography>}
            </Box>
          )}
        </SurfaceCard>
      </Box>
    </AppLayout>
  );
}
