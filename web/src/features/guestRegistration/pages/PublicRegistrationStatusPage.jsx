import { useCallback, useEffect, useState } from "react";
import { useLocation, useParams } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import apiClient from "../../../shared/api/apiClient";
import SurfaceCard from "../../../shared/components/ui/SurfaceCard";

export default function PublicRegistrationStatusPage() {
  const { publicReference } = useParams();
  const location = useLocation();
  const [secret, setSecret] = useState(
    () => new URLSearchParams(location.hash.slice(1)).get("secret") || "",
  );
  const [registration, setRegistration] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const checkStatus = useCallback(async (credential) => {
    if (!credential) {
      setError("Enter the secret from your registration email.");
      return;
    }
    setLoading(true);
    setError("");
    try {
      const { data } = await apiClient.post(
        `/api/public/registrations/${encodeURIComponent(publicReference)}/status`,
        { secret: credential },
      );
      setRegistration(data);
      setSecret(credential);
    } catch (requestError) {
      setError(requestError.message || "Registration status could not be loaded.");
    } finally {
      setLoading(false);
    }
  }, [publicReference]);

  useEffect(() => {
    const credential = new URLSearchParams(location.hash.slice(1)).get("secret") || "";
    if (!credential) return undefined;
    const timeout = window.setTimeout(() => void checkStatus(credential), 0);
    return () => window.clearTimeout(timeout);
  }, [checkStatus, location.hash]);

  const respond = async (response) => {
    setLoading(true);
    setError("");
    try {
      const { data } = await apiClient.post(
        `/api/public/registrations/${encodeURIComponent(publicReference)}/rsvp`,
        { secret, response },
      );
      setRegistration(data);
    } catch (requestError) {
      setError(requestError.message || "Your RSVP could not be submitted.");
    } finally {
      setLoading(false);
    }
  };

  const submitSecret = (event) => {
    event.preventDefault();
    void checkStatus(secret);
  };

  return (
    <Container maxWidth="sm" sx={{ py: 6 }}>
      <SurfaceCard sx={{ p: { xs: 2.5, sm: 4 } }}>
        <Typography variant="h4" sx={{ fontWeight: 800, mb: 1 }}>
          Registration status
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 3 }}>
          Reference: {publicReference}
        </Typography>
        {!registration && (
          <Box component="form" onSubmit={submitSecret}>
            <Stack spacing={2}>
              <TextField
                label="Registration secret"
                type="password"
                required
                value={secret}
                onChange={(event) => setSecret(event.target.value)}
              />
              <Button type="submit" variant="contained" disabled={loading}>
                Check status
              </Button>
            </Stack>
          </Box>
        )}
        {loading && (
          <Box sx={{ display: "grid", placeItems: "center", py: 3 }}>
            <CircularProgress />
          </Box>
        )}
        {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
        {registration && !loading && (
          <Stack spacing={2} sx={{ mt: 2 }}>
            <Alert severity={registration.status === "CONFIRMED" ? "success" : "info"}>
              Status: {registration.status}
              {registration.rsvpStatus && ` · RSVP: ${registration.rsvpStatus}`}
            </Alert>
            {registration.qrPngBase64 && (
              <Box
                component="img"
                alt="Event check-in QR code"
                src={`data:image/png;base64,${registration.qrPngBase64}`}
                sx={{ display: "block", width: 220, maxWidth: "100%", mx: "auto" }}
              />
            )}
            {registration.status === "CONFIRMED" && registration.invitationToken && (
              <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                <Button variant="contained" onClick={() => respond("ACCEPTED")} disabled={loading}>
                  Accept
                </Button>
                <Button variant="outlined" onClick={() => respond("MAYBE")} disabled={loading}>
                  Maybe
                </Button>
                <Button color="error" variant="outlined" onClick={() => respond("DECLINED")} disabled={loading}>
                  Decline
                </Button>
              </Stack>
            )}
            <Button variant="text" onClick={() => void checkStatus(secret)} disabled={loading}>
              Refresh status
            </Button>
          </Stack>
        )}
      </SurfaceCard>
    </Container>
  );
}
