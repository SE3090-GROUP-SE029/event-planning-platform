import { useState } from 'react';
import { Box, InputBase, Chip, IconButton, Avatar } from '@mui/material';

export default function TopSearchNavbar({ user, onSearch }) {
  const [activeFilter, setActiveFilter] = useState('Events');
  const filters = ['Events', 'Venues', 'Vendors', 'Schedules'];

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 3,
        mb: 3,
        flexWrap: { xs: 'wrap', md: 'nowrap' },
      }}
    >
      {/* Pill Search Bar */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          backgroundColor: 'rgba(255, 255, 255, 0.75)',
          backdropFilter: 'blur(10px)',
          borderRadius: 9999,
          p: '6px 8px 6px 8px',
          flex: 1,
          maxWidth: { xs: '100%', md: 680 },
          boxShadow: '0 2px 12px rgba(40, 30, 20, 0.04)',
          border: '1px solid rgba(0, 0, 0, 0.04)',
        }}
      >
        {/* Pink Search Icon Pill */}
        <Box
          sx={{
            width: 34,
            height: 34,
            borderRadius: '50%',
            backgroundColor: '#F9BFD8',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#19191C',
            mr: 1.5,
            flexShrink: 0,
          }}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
        </Box>

        {/* Search Input Field */}
        <InputBase
          placeholder="Search anything (events, venues, attendees)..."
          onChange={(e) => onSearch && onSearch(e.target.value)}
          sx={{
            flex: 1,
            fontSize: '13.5px',
            color: '#1E1E22',
            '& input::placeholder': {
              color: '#8E8E98',
              opacity: 1,
            },
          }}
        />

        {/* Filter Pills */}
        <Box sx={{ display: { xs: 'none', sm: 'flex' }, alignItems: 'center', gap: 0.8, ml: 1 }}>
          <Box sx={{ fontSize: '11px', color: '#8E8E98', mr: 0.5, fontWeight: 500 }}>in:</Box>
          {filters.map((filter) => {
            const isSelected = activeFilter === filter;
            return (
              <Chip
                key={filter}
                label={filter}
                size="small"
                onClick={() => setActiveFilter(filter)}
                sx={{
                  height: 26,
                  fontSize: '11.5px',
                  fontWeight: isSelected ? 600 : 400,
                  backgroundColor: isSelected ? '#19191C' : 'rgba(0,0,0,0.04)',
                  color: isSelected ? '#FFFFFF' : '#636369',
                  borderRadius: 9999,
                  cursor: 'pointer',
                  '&:hover': {
                    backgroundColor: isSelected ? '#2A2A32' : 'rgba(0,0,0,0.08)',
                  },
                }}
              />
            );
          })}
        </Box>
      </Box>

      {/* Right Utility Dark Pill Buttons */}
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.2 }}>
        {/* Bell Button */}
        <IconButton
          sx={{
            backgroundColor: '#19191C',
            color: '#FFFFFF',
            width: 40,
            height: 40,
            borderRadius: '50%',
            '&:hover': { backgroundColor: '#2E2E36' },
          }}
        >
          <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
            <path d="M13.73 21a2 2 0 0 1-3.46 0" />
          </svg>
        </IconButton>

        {/* Settings Button */}
        <IconButton
          sx={{
            backgroundColor: '#19191C',
            color: '#FFFFFF',
            width: 40,
            height: 40,
            borderRadius: '50%',
            '&:hover': { backgroundColor: '#2E2E36' },
          }}
        >
          <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="3" />
            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" />
          </svg>
        </IconButton>

        {/* User Avatar */}
        <Avatar
          sx={{
            width: 40,
            height: 40,
            backgroundColor: '#19191C',
            color: '#F9BFD8',
            fontWeight: 700,
            fontSize: '14px',
            border: '2px solid #FFFFFF',
            cursor: 'pointer',
          }}
        >
          {user?.email ? user.email.charAt(0).toUpperCase() : 'O'}
        </Avatar>
      </Box>
    </Box>
  );
}
