import { Avatar, Box, InputBase, Typography } from '@mui/material';
import { tokens } from '../../theme/tokens';

export default function TopSearchNavbar({ user, onSearch, title, subtitle }) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, mb: 3.5 }}>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          backgroundColor: tokens.colors.surface,
          borderRadius: tokens.radius.pill,
          px: 2,
          py: 1,
          flex: 1,
          maxWidth: 680,
          border: `1px solid ${tokens.colors.borderLight}`,
          boxShadow: tokens.shadows.soft,
        }}
      >
        <Box sx={{ mr: 1.25, color: tokens.colors.textSecondary, fontSize: 16 }}>⌕</Box>
        <InputBase
          fullWidth
          placeholder="Search events"
          onChange={(event) => onSearch?.(event.target.value)}
          sx={{ fontSize: 14, color: tokens.colors.textPrimary }}
        />
      </Box>

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
        {(title || subtitle) && (
          <Box sx={{ textAlign: 'right', display: { xs: 'none', sm: 'block' } }}>
            {title && <Typography variant="subtitle2" sx={{ fontWeight: 700, color: tokens.colors.textPrimary }}>{title}</Typography>}
            {subtitle && <Typography variant="caption" sx={{ color: tokens.colors.textSecondary }}>{subtitle}</Typography>}
          </Box>
        )}
        {user?.roles?.[0] && (
          <Box
            sx={{
              display: { xs: 'none', md: 'inline-flex' },
              px: 1.5,
              py: 0.5,
              borderRadius: 9999,
              backgroundColor: tokens.colors.pastelPinkLight,
              color: tokens.colors.pastelPinkText,
              fontSize: '0.72rem',
              fontWeight: 800,
              letterSpacing: '0.04em',
              textTransform: 'uppercase',
            }}
          >
            {user.roles[0].replace('_', ' ')}
          </Box>
        )}
        <Avatar
          sx={{
            width: 42,
            height: 42,
            backgroundColor: tokens.colors.pastelBlue,
            color: tokens.colors.pastelBlueText,
            fontWeight: 800,
            border: `2px solid ${tokens.colors.surface}`,
            boxShadow: tokens.shadows.soft,
          }}
        >
          {user?.email?.charAt(0).toUpperCase() || '?'}
        </Avatar>
      </Box>
    </Box>
  );
}
