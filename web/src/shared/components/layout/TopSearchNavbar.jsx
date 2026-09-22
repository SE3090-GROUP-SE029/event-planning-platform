import { Avatar, Box, InputBase, Typography } from '@mui/material';

export default function TopSearchNavbar({ user, onSearch, title, subtitle }) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, mb: 3.5 }}>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          backgroundColor: '#FFFFFF',
          borderRadius: 9999,
          px: 2,
          py: 1,
          flex: 1,
          maxWidth: 680,
          border: '1px solid rgba(25,25,28,0.06)',
          boxShadow: '0 6px 18px rgba(30, 25, 22, 0.04)',
        }}
      >
        <Box sx={{ mr: 1.25, color: '#616168', fontSize: 16 }}>⌕</Box>
        <InputBase
          fullWidth
          placeholder="Search events"
          onChange={(event) => onSearch?.(event.target.value)}
          sx={{ fontSize: 14, color: '#1E1E22' }}
        />
      </Box>

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
        {(title || subtitle) && (
          <Box sx={{ textAlign: 'right', display: { xs: 'none', sm: 'block' } }}>
            {title && <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#1E1E22' }}>{title}</Typography>}
            {subtitle && <Typography variant="caption" sx={{ color: '#636369' }}>{subtitle}</Typography>}
          </Box>
        )}
        <Avatar
          sx={{
            width: 42,
            height: 42,
            background: 'linear-gradient(135deg, #19191C 0%, #2A2A30 100%)',
            color: '#F9BFD8',
            fontWeight: 800,
            border: '2px solid rgba(255,255,255,0.6)',
          }}
        >
          {user?.email?.charAt(0).toUpperCase() || '?'}
        </Avatar>
      </Box>
    </Box>
  );
}
