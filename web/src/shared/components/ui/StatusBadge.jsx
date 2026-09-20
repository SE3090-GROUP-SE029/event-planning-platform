import { Chip } from '@mui/material';

const STATUS_STYLES = {
  DRAFT: { bg: '#58534b', color: '#F7F3E9' },
  PLANNING: { bg: '#45396d', color: '#EDE7FF' },
  CONFIRMED: { bg: '#214b3b', color: '#E2F5EA' },
  COMPLETED: { bg: '#2d4d73', color: '#E9F3FF' },
  CANCELLED: { bg: '#7a3b37', color: '#FDE4E1' },
  APPROVED: { bg: '#214b3b', color: '#E2F5EA' },
  PENDING: { bg: '#6e5221', color: '#FFF3D6' },
  REJECTED: { bg: '#7a3b37', color: '#FDE4E1' },
};

export default function StatusBadge({ label, status }) {
  const normalizedStatus = (status || label || 'DRAFT').toUpperCase();
  const style = STATUS_STYLES[normalizedStatus] || { bg: '#58534b', color: '#F7F3E9' };

  return (
    <Chip
      label={label || normalizedStatus.replaceAll('_', ' ')}
      size="small"
      sx={{
        backgroundColor: style.bg,
        color: style.color,
        borderRadius: '999px',
        fontWeight: 700,
        letterSpacing: '0.02em',
        px: 0.25,
        py: 0.2,
      }}
    />
  );
}
