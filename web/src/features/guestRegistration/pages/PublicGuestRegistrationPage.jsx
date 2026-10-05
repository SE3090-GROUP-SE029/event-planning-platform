import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
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

export default function PublicGuestRegistrationPage() {
  const { publicId } = useParams();
  const navigate = useNavigate();
  const [form, setForm] = useState(null);
  const [details, setDetails] = useState({
    fullName: "",
    emailAddress: "",
    organisation: "",
    phoneNumber: "",
  });
  const [answers, setAnswers] = useState({});
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    apiClient
      .get(`/api/public/registration-forms/${encodeURIComponent(publicId)}`)
      .then(({ data }) => {
        if (active) {
          setForm(data);
          setLoading(false);
        }
      })
      .catch((requestError) => {
        if (active) {
          setError(requestError.message || "Registration form could not be loaded.");
          setLoading(false);
        }
      });
    return () => {
      active = false;
    };
  }, [publicId]);

  const updateDetails = (event) => {
    setDetails((current) => ({ ...current, [event.target.name]: event.target.value }));
  };

  const submit = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    setError("");
    try {
      const payload = {
        ...details,
        answers: (form.questions || [])
          .filter((question) => (answers[question.id] || "").trim())
          .map((question) => ({
            questionId: question.id,
            answer: answers[question.id].trim(),
          })),
      };
      const { data } = await apiClient.post(
        `/api/public/registration-forms/${encodeURIComponent(publicId)}/registrations`,
        payload,
      );
      navigate(
        `/guest/status/${encodeURIComponent(data.publicReference)}#secret=${encodeURIComponent(data.statusSecret)}`,
        { state: { eventName: form.eventName } },
      );
    } catch (requestError) {
      setError(requestError.message || "Registration could not be submitted.");
      setSubmitting(false);
    }
  };

  return (
    <Container maxWidth="sm" sx={{ py: 6 }}>
      <SurfaceCard sx={{ p: { xs: 2.5, sm: 4 } }}>
        {loading ? (
          <Box sx={{ display: "grid", placeItems: "center", py: 6 }}>
            <CircularProgress />
          </Box>
        ) : error && !form ? (
          <Alert severity="error">{error}</Alert>
        ) : (
          <>
            <Typography variant="h4" sx={{ fontWeight: 800, mb: 1 }}>
              {form.eventName}
            </Typography>
            {form.description && (
              <Typography color="text.secondary" sx={{ mb: 2 }}>
                {form.description}
              </Typography>
            )}
            <Typography color="text.secondary" sx={{ mb: 3 }}>
              {form.startsAt ? new Date(form.startsAt).toLocaleString() : ""}
              {form.location ? ` · ${form.location}` : ""}
            </Typography>
            {!form.isOpen ? (
              <Alert severity="info">Registration is currently closed.</Alert>
            ) : (
              <Box component="form" onSubmit={submit}>
                <Stack spacing={2}>
                  <TextField
                    name="fullName"
                    label="Full name"
                    required
                    inputProps={{ maxLength: 200 }}
                    value={details.fullName}
                    onChange={updateDetails}
                  />
                  <TextField
                    name="emailAddress"
                    label="Email address"
                    type="email"
                    required
                    inputProps={{ maxLength: 254 }}
                    value={details.emailAddress}
                    onChange={updateDetails}
                  />
                  {form.optionalFields?.includes("organisation") && (
                    <TextField
                      name="organisation"
                      label="Organisation (optional)"
                      inputProps={{ maxLength: 200 }}
                      value={details.organisation}
                      onChange={updateDetails}
                    />
                  )}
                  {form.optionalFields?.includes("phoneNumber") && (
                    <TextField
                      name="phoneNumber"
                      label="Phone number (optional)"
                      inputProps={{ maxLength: 40 }}
                      value={details.phoneNumber}
                      onChange={updateDetails}
                    />
                  )}
                  {(form.questions || []).map((question) => (
                    <TextField
                      key={question.id}
                      label={question.question}
                      required={question.required}
                      multiline
                      minRows={2}
                      inputProps={{ maxLength: 4000 }}
                      value={answers[question.id] || ""}
                      onChange={(changeEvent) =>
                        setAnswers((current) => ({
                          ...current,
                          [question.id]: changeEvent.target.value,
                        }))
                      }
                    />
                  ))}
                  {error && <Alert severity="error">{error}</Alert>}
                  <Button type="submit" variant="contained" disabled={submitting}>
                    {submitting ? "Submitting…" : "Submit registration"}
                  </Button>
                </Stack>
              </Box>
            )}
          </>
        )}
      </SurfaceCard>
    </Container>
  );
}
