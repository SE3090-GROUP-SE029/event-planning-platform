import { Button, CircularProgress } from '@mui/material';

export default function VendorApprovalAction({ status, loading, disabled, onApprove }) {
  if (String(status || '').toUpperCase() !== 'PENDING') return null;

  return (
    <Button
      size="small"
      variant="contained"
      disabled={disabled || loading}
      onClick={onApprove}
      startIcon={loading ? <CircularProgress size={16} color="inherit" /> : null}
    >
      {loading ? 'Approving…' : 'Approve Vendor'}
    </Button>
  );
}
