import { useState } from 'react';
import { Box, Typography, Button, IconButton, Avatar, AvatarGroup } from '@mui/material';

const DAYS_OF_WEEK = ['MO', 'TU', 'WE', 'TH', 'FR', 'SA', 'SU'];

const CALENDAR_DAYS = [
  { day: 1, inMonth: true },
  { day: 2, inMonth: true },
  { day: 3, inMonth: true },
  { day: 4, inMonth: true },
  { day: 5, inMonth: true },
  { day: 6, inMonth: true },
  { day: 7, inMonth: true },
  { day: 8, inMonth: true },
  { day: 9, inMonth: true },
  { day: 10, inMonth: true },
  { day: 11, inMonth: true },
  { day: 12, inMonth: true },
  { day: 13, inMonth: true },
  { day: 14, inMonth: true },
  { day: 15, inMonth: true, isSelected: true },
  { day: 16, inMonth: true },
  { day: 17, inMonth: true },
  { day: 18, inMonth: true },
  { day: 19, inMonth: true },
  { day: 20, inMonth: true },
  { day: 21, inMonth: true },
  { day: 22, inMonth: true },
  { day: 23, inMonth: true },
  { day: 24, inMonth: true },
  { day: 25, inMonth: true },
  { day: 26, inMonth: true },
  { day: 27, inMonth: true },
  { day: 28, inMonth: true },
  { day: 29, inMonth: true },
  { day: 30, inMonth: true },
  { day: 31, inMonth: true },
];

const TIMELINE_EVENTS = [
  {
    time: '07:00',
    title: 'Venue Inspection',
    subtitle: 'Grand Ballroom, Hall A',
    color: '#F9BFD8',
    textColor: '#481931',
    iconType: 'venue',
  },
  {
    time: '07:30',
    title: 'Catering Tasting',
    subtitle: 'Kitchen Suite, Level 2',
    color: '#BCD8F0',
    textColor: '#153251',
    iconType: 'catering',
  },
  {
    time: '08:12',
    title: 'Team Daily Planning',
    subtitle: 'Conference Room 200',
    color: '#FEE388',
    textColor: '#3E340B',
    isCurrent: true,
    avatars: ['M', 'S', 'A', 'D'],
    duration: '08:00 - 09:00',
  },
  {
    time: '09:00',
    title: 'Audio-Visual Sound Check',
    subtitle: 'Main Stage, West Wing',
    color: '#F9BFD8',
    textColor: '#481931',
    iconType: 'sound',
  },
];

export default function CalendarTimelineRail({ onAddEvent, onViewDetails }) {
  const [selectedDay, setSelectedDay] = useState(15);

  return (
    <Box
      sx={{
        width: { xs: '100%', lg: 320 },
        flexShrink: 0,
        display: 'flex',
        flexDirection: 'column',
        gap: 3,
      }}
    >
      {/* Month Navigation & Mini Calendar Card */}
      <Box
        sx={{
          backgroundColor: '#FFFFFF',
          borderRadius: '24px',
          p: '20px 18px',
          boxShadow: '0 4px 24px rgba(35, 25, 15, 0.04)',
        }}
      >
        {/* Month Header */}
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
          <IconButton size="small" sx={{ color: '#1E1E22' }}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
              <polyline points="15 18 9 12 15 6" />
            </svg>
          </IconButton>

          <Box
            sx={{
              backgroundColor: '#FDEEF5',
              color: '#481931',
              px: 2,
              py: 0.5,
              borderRadius: 9999,
              fontWeight: 700,
              fontSize: '13px',
            }}
          >
            May 2026
          </Box>

          <IconButton size="small" sx={{ color: '#1E1E22' }}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
              <polyline points="9 18 15 12 9 6" />
            </svg>
          </IconButton>
        </Box>

        {/* Days of Week */}
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', gap: 0.5, mb: 1 }}>
          {DAYS_OF_WEEK.map((d) => (
            <Typography
              key={d}
              sx={{
                textAlign: 'center',
                fontSize: '10.5px',
                fontWeight: 600,
                color: '#8F8F96',
              }}
            >
              {d}
            </Typography>
          ))}
        </Box>

        {/* Calendar Grid */}
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', gap: 0.5 }}>
          {CALENDAR_DAYS.map((item) => {
            const isSel = selectedDay === item.day;
            return (
              <Box
                key={item.day}
                onClick={() => setSelectedDay(item.day)}
                sx={{
                  aspectRatio: '1',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '12px',
                  fontWeight: isSel ? 700 : 500,
                  borderRadius: '50%',
                  cursor: 'pointer',
                  backgroundColor: isSel ? '#F9BFD8' : 'transparent',
                  color: isSel ? '#481931' : '#1E1E22',
                  transition: 'all 0.15s ease',
                  '&:hover': {
                    backgroundColor: isSel ? '#F9BFD8' : 'rgba(0,0,0,0.04)',
                  },
                }}
              >
                {item.day}
              </Box>
            );
          })}
        </Box>

        {/* + Add Event Obsidian Pill Button */}
        <Box sx={{ display: 'flex', gap: 1, mt: 2.5 }}>
          <Button
            fullWidth
            variant="contained"
            onClick={onAddEvent}
            sx={{
              backgroundColor: '#19191C',
              color: '#FFFFFF',
              borderRadius: 9999,
              py: 1.1,
              fontSize: '13px',
              fontWeight: 600,
              '&:hover': { backgroundColor: '#2E2E36' },
            }}
          >
            + Add event
          </Button>

          <IconButton
            sx={{
              border: '1px solid rgba(0,0,0,0.08)',
              borderRadius: '50%',
              width: 38,
              height: 38,
            }}
          >
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="23 4 23 10 17 10" />
              <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10" />
            </svg>
          </IconButton>
        </Box>
      </Box>

      {/* Today's Timeline Card */}
      <Box
        sx={{
          backgroundColor: '#FFFFFF',
          borderRadius: '24px',
          p: '20px 18px',
          boxShadow: '0 4px 24px rgba(35, 25, 15, 0.04)',
          display: 'flex',
          flexDirection: 'column',
          flex: 1,
        }}
      >
        {/* Header */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2.5 }}>
          <Box>
            <Typography sx={{ fontSize: '15px', fontWeight: 700, color: '#1E1E22' }}>
              May 15
            </Typography>
            <Typography sx={{ fontSize: '11px', color: '#8F8F96', fontWeight: 500 }}>
              Today's timeline
            </Typography>
          </Box>

          <Box
            sx={{
              backgroundColor: '#19191C',
              color: '#FFFFFF',
              borderRadius: 9999,
              px: 1.5,
              py: 0.3,
              fontSize: '11px',
              fontWeight: 600,
              display: 'flex',
              alignItems: 'center',
              gap: 0.5,
              cursor: 'pointer',
            }}
          >
            All
            <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3">
              <polyline points="6 9 12 15 18 9" />
            </svg>
          </Box>
        </Box>

        {/* Timeline Items */}
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, position: 'relative', mb: 3 }}>
          {TIMELINE_EVENTS.map((event, idx) => (
            <Box key={idx} sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
              {/* Time Label */}
              <Typography
                sx={{
                  fontSize: '11px',
                  fontWeight: 600,
                  color: event.isCurrent ? '#E0B624' : '#8F8F96',
                  width: 38,
                  pt: 0.8,
                }}
              >
                {event.time}
              </Typography>

              {/* Event Container */}
              <Box
                sx={{
                  flex: 1,
                  backgroundColor: event.isCurrent ? '#FEF8E4' : '#FAF7EF',
                  border: event.isCurrent ? '1px dashed #E0B624' : '1px solid transparent',
                  borderRadius: '16px',
                  p: '10px 12px',
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1.5,
                }}
              >
                {/* Icon Badge */}
                <Box
                  sx={{
                    width: 32,
                    height: 32,
                    borderRadius: '50%',
                    backgroundColor: event.color,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: event.textColor,
                    flexShrink: 0,
                  }}
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                    <circle cx="12" cy="12" r="10" />
                    <polyline points="12 6 12 12 16 14" />
                  </svg>
                </Box>

                {/* Details */}
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Typography
                    sx={{
                      fontSize: '12.5px',
                      fontWeight: 600,
                      color: '#1E1E22',
                      whiteSpace: 'nowrap',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                    }}
                  >
                    {event.title}
                  </Typography>
                  <Typography
                    sx={{
                      fontSize: '10.5px',
                      color: '#8F8F96',
                      whiteSpace: 'nowrap',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                    }}
                  >
                    {event.subtitle}
                  </Typography>

                  {/* Avatars if team event */}
                  {event.avatars && (
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 1 }}>
                      <AvatarGroup max={4} sx={{ '& .MuiAvatar-root': { width: 20, height: 20, fontSize: 10 } }}>
                        <Avatar sx={{ bgcolor: '#F9BFD8', color: '#481931' }}>M</Avatar>
                        <Avatar sx={{ bgcolor: '#BCD8F0', color: '#153251' }}>S</Avatar>
                        <Avatar sx={{ bgcolor: '#C4DDB8', color: '#20361A' }}>A</Avatar>
                        <Avatar sx={{ bgcolor: '#FEE388', color: '#3E340B' }}>D</Avatar>
                      </AvatarGroup>
                      <Typography sx={{ fontSize: '10px', color: '#8F8F96', fontWeight: 500 }}>
                        {event.duration}
                      </Typography>
                    </Box>
                  )}
                </Box>
              </Box>
            </Box>
          ))}
        </Box>

        {/* View All Details Pill Button */}
        <Button
          fullWidth
          variant="contained"
          onClick={onViewDetails}
          sx={{
            mt: 'auto',
            backgroundColor: '#19191C',
            color: '#FFFFFF',
            borderRadius: 9999,
            py: 1.1,
            fontSize: '13px',
            fontWeight: 600,
            '&:hover': { backgroundColor: '#2E2E36' },
          }}
        >
          View all details
        </Button>
      </Box>
    </Box>
  );
}
