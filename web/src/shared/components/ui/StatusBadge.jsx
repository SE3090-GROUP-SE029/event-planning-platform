import { Chip } from '@mui/material';

import { tokens } from '../../theme/tokens';

const STATUS_STYLES = {
  DRAFT: { bg: tokens.colors.surfaceMuted, color: tokens.colors.textSecondary },
  PLANNING: { bg: tokens.colors.pastelBlueLight, color: tokens.colors.pastelBlueText },
  CONFIRMED: { bg: tokens.colors.pastelGreenLight, color: tokens.colors.pastelGreenText },
  COMPLETED: { bg: tokens.colors.pastelLavenderLight, color: tokens.colors.pastelLavenderText },
  CANCELLED: { bg: tokens.colors.pastelPinkLight, color: tokens.colors.pastelPinkText },
  APPROVED: { bg: tokens.colors.pastelGreenLight, color: tokens.colors.pastelGreenText },
  PENDING: { bg: tokens.colors.pastelPeachLight, color: tokens.colors.pastelPeachText },
  REJECTED: { bg: tokens.colors.pastelPinkLight, color: tokens.colors.pastelPinkText },
};

export default function StatusBadge({ label, status }) {
  const normalizedStatus = (status || label || 'DRAFT').toUpperCase();
  const style = STATUS_STYLES[normalizedStatus] || STATUS_STYLES.DRAFT;

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
