import { Box, Avatar, InputBase } from '@mui/material';

export default function TopSearchNavbar({ user, onSearch }) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, mb: 3 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', backgroundColor: '#FFFFFF', borderRadius: 9999, px: 2, py: 1, flex: 1, maxWidth: 680, border: '1px solid rgba(0,0,0,0.06)' }}>
        <InputBase
          fullWidth
          placeholder="Search events"
          onChange={(event) => onSearch?.(event.target.value)}
          sx={{ fontSize: 14 }}
        />
      </Box>
      <Avatar sx={{ width: 40, height: 40, backgroundColor: '#19191C', color: '#F9BFD8', fontWeight: 700 }}>
        {user?.email?.charAt(0).toUpperCase() || '?'}
      </Avatar>
    </Box>
  );
}
