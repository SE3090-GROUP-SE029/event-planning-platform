import { useRef, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  List,
  ListItem,
  ListItemText,
  Stack,
  Typography,
} from "@mui/material";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import FileUploadOutlinedIcon from "@mui/icons-material/FileUploadOutlined";
import CheckCircleOutlinedIcon from "@mui/icons-material/CheckCircleOutlined";
import ErrorOutlinedIcon from "@mui/icons-material/ErrorOutlined";
import AppLayout from "../../../shared/components/layout/AppLayout";
import SurfaceCard from "../../../shared/components/ui/SurfaceCard";
import { useUploadGuestList } from "../api/guestListApi";
import { tokens } from "../../../shared/theme/tokens";

const ACCEPTED_TYPES = ".csv,text/csv,text/plain,application/octet-stream";

function StatBox({ label, value, variant }) {
  return (
    <SurfaceCard variant={variant} sx={{ p: 2.5, minWidth: 120, textAlign: "center" }}>
      <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: "-0.04em" }}>
        {value}
      </Typography>
      <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>
        {label}
      </Typography>
    </SurfaceCard>
  );
}

export default function GuestListUploadPage() {
  const { id: eventId } = useParams();
  const navigate = useNavigate();
  const fileInputRef = useRef(null);
  const [selectedFile, setSelectedFile] = useState(null);
  const [dragOver, setDragOver] = useState(false);
  const mutation = useUploadGuestList(eventId);

  const handleFileChange = (e) => {
    const file = e.target.files?.[0];
    if (file) {
      setSelectedFile(file);
      mutation.reset();
    }
  };

  const handleDrop = (e) => {
    e.preventDefault();
    setDragOver(false);
    const file = e.dataTransfer.files?.[0];
    if (file) {
      setSelectedFile(file);
      mutation.reset();
    }
  };

  const handleUpload = () => {
    if (!selectedFile) return;
    mutation.mutate(selectedFile);
  };

  const result = mutation.data;

  return (
    <AppLayout
      activeTab="events"
      title="Guest List Upload"
      subtitle="Import guests from a CSV file"
    >
      <Button
        startIcon={<ArrowBackOutlinedIcon />}
        onClick={() => navigate("/admin/events")}
        sx={{ mb: 2, borderRadius: 9999, px: 2.5 }}
      >
        Back to events
      </Button>

      <Box sx={{ mb: 3 }}>
        <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: "-0.04em", mb: 0.5 }}>
          Upload Guest List
        </Typography>
        <Typography color="text.secondary">
          Upload a CSV file to register multiple guests at once. Each valid guest enters
          the same AI review → capacity → invitation pipeline as public registrations.
        </Typography>
      </Box>

      {/* Instructions card */}
      <SurfaceCard variant="analytics" sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" sx={{ fontWeight: 800, mb: 1.5 }}>
          CSV Format Requirements
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          The first row must be a header row. Required columns:
        </Typography>
        <Stack direction="row" flexWrap="wrap" gap={1} sx={{ mb: 1.5 }}>
          {["fullName", "emailAddress"].map((col) => (
            <Chip key={col} label={col} size="small"
              sx={{ bgcolor: tokens.colors.pastelBlue, color: tokens.colors.pastelBlueText, fontWeight: 700 }} />
          ))}
          {["organisation", "phoneNumber"].map((col) => (
            <Chip key={col} label={`${col} (optional)`} size="small"
              sx={{ bgcolor: tokens.colors.surfaceMuted, color: tokens.colors.textSecondary }} />
          ))}
        </Stack>
        <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 0.5 }}>
          • Maximum 1,000 guest rows per file (max 5 MB)
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 0.5 }}>
          • The registration form for this event must be published before uploading
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ display: "block" }}>
          • Duplicate emails (within the file or already registered) are skipped automatically
        </Typography>
      </SurfaceCard>

      {/* Upload dropzone */}
      <SurfaceCard
        sx={{
          p: 4,
          mb: 3,
          border: `2px dashed ${dragOver ? tokens.colors.pastelBlue : tokens.colors.borderLight}`,
          bgcolor: dragOver ? tokens.colors.pastelBlueLight : tokens.colors.surface,
          cursor: "pointer",
          transition: "all 0.2s ease",
          textAlign: "center",
        }}
        onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
      >
        <input
          ref={fileInputRef}
          type="file"
          accept={ACCEPTED_TYPES}
          style={{ display: "none" }}
          onChange={handleFileChange}
        />
        <FileUploadOutlinedIcon sx={{ fontSize: 48, color: tokens.colors.textMuted, mb: 1 }} />
        <Typography variant="h6" sx={{ fontWeight: 700, mb: 0.5 }}>
          {selectedFile ? selectedFile.name : "Drop your CSV here, or click to browse"}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {selectedFile
            ? `${(selectedFile.size / 1024).toFixed(1)} KB selected`
            : "Accepted: .csv — max 5 MB, up to 1,000 rows"}
        </Typography>
      </SurfaceCard>

      {mutation.isError && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
          {mutation.error?.message || "Upload failed. Please try again."}
        </Alert>
      )}

      {/* Upload button */}
      <Stack direction="row" gap={2} sx={{ mb: 3 }}>
        <Button
          variant="contained"
          onClick={handleUpload}
          disabled={!selectedFile || mutation.isPending}
          startIcon={mutation.isPending ? <CircularProgress size={18} color="inherit" /> : <FileUploadOutlinedIcon />}
          sx={{ borderRadius: 9999, px: 3, bgcolor: tokens.colors.obsidian, "&:hover": { bgcolor: tokens.colors.obsidianHover } }}
        >
          {mutation.isPending ? "Uploading…" : "Upload Guest List"}
        </Button>
        {selectedFile && (
          <Button
            variant="text"
            onClick={() => { setSelectedFile(null); mutation.reset(); if (fileInputRef.current) fileInputRef.current.value = ""; }}
            sx={{ borderRadius: 9999 }}
          >
            Clear
          </Button>
        )}
      </Stack>

      {/* Result summary */}
      {result && (
        <>
          <Divider sx={{ mb: 3 }} />
          <Typography variant="h5" sx={{ fontWeight: 800, mb: 2 }}>
            Upload Summary
          </Typography>
          <Stack direction={{ xs: "column", sm: "row" }} flexWrap="wrap" gap={2} sx={{ mb: 3 }}>
            <StatBox label="Total Rows" value={result.totalRows} variant="default" />
            <StatBox label="Registered" value={result.successfulRows} variant="task" />
            <StatBox label="Already Registered" value={result.alreadyRegisteredRows} variant="analytics" />
            <StatBox label="In-File Duplicates" value={result.duplicateRows} variant="aiPlan" />
            <StatBox label="Failed" value={result.failedRows} variant="vendor" />
          </Stack>

          {result.successfulRows > 0 && (
            <Alert
              icon={<CheckCircleOutlinedIcon />}
              severity="success"
              sx={{ mb: 2, borderRadius: 3 }}
            >
              {result.successfulRows} guest{result.successfulRows !== 1 ? "s" : ""} registered successfully and queued for AI review.
            </Alert>
          )}

          {result.errors?.length > 0 && (
            <SurfaceCard variant="vendor" sx={{ p: 3 }}>
              <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 1.5 }}>
                <ErrorOutlinedIcon color="error" />
                <Typography variant="h6" sx={{ fontWeight: 800 }}>
                  Row Errors ({result.errors.length})
                </Typography>
              </Box>
              <List dense disablePadding>
                {result.errors.map((err, i) => (
                  <ListItem key={i} disablePadding sx={{ py: 0.25 }}>
                    <ListItemText
                      primary={
                        <Typography variant="body2">
                          <Box component="span" sx={{ fontWeight: 700 }}>
                            {err.rowNumber > 0 ? `Row ${err.rowNumber} — ` : "Header — "}
                          </Box>
                          {err.message}
                        </Typography>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            </SurfaceCard>
          )}
        </>
      )}
    </AppLayout>
  );
}
