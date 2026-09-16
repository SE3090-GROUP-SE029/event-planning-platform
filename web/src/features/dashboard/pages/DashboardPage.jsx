import { useState } from 'react';
import { Box, Typography, Button, Chip } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';
import PastelStatCard from '../../../shared/components/ui/PastelStatCard';
import CalendarTimelineRail from '../components/CalendarTimelineRail';

export default function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState('dashboard');
  const [selectedEventIndex, setSelectedEventIndex] = useState(0);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const displayName = user?.email ? user.email.split('@')[0] : 'Olivia';

  // Event list data matching the reference style
  const eventList = [
    {
      id: 1,
      title: 'Global Tech Summit 2026',
      category: 'Keynote & Exhibition',
      time: '09:15 AM',
      color: '#F9BFD8',
      textColor: '#481931',
      code: 'EVT-7821',
      tags: ['Keynote', 'Audio Visual', 'Catering'],
      coordinator: 'Dr. Everly',
      date: '15 May 2026',
      notes: 'Main auditorium capacity verified at 850 attendees. Lighting checks completed.',
      status: 'On Schedule',
    },
    {
      id: 2,
      title: 'Samantha & David Wedding',
      category: 'Reception & Banquet',
      time: '09:35 AM',
      color: '#BCD8F0',
      textColor: '#153251',
      code: 'EVT-9043',
      tags: ['Floral', 'Banquet', 'Photography'],
      coordinator: 'Elena Vance',
      date: '16 May 2026',
      notes: 'Outdoor garden floral arch and stage seating arranged.',
      status: 'Final Review',
    },
    {
      id: 3,
      title: 'Healthcare Innovation Forum',
      category: 'Virtual Workshop',
      time: '11:00 AM',
      color: '#E5D4F7',
      textColor: '#391652',
      code: 'EVT-5512',
      tags: ['Virtual', 'Livestream', 'Panel'],
      coordinator: 'Marcus Reed',
      date: '17 May 2026',
      notes: 'High-speed fiber connection tests passed. 3 keynote speakers confirmed.',
      status: 'Ready',
    },
    {
      id: 4,
      title: 'Spring Symphony Gala',
      category: 'Concert & Dinner',
      time: '01:45 PM',
      color: '#C4DDB8',
      textColor: '#20361A',
      code: 'EVT-3329',
      tags: ['Acoustics', 'VIP Dinner', 'Ticketing'],
      coordinator: 'Sarah Jenkins',
      date: '18 May 2026',
      notes: 'Orchestra rehearsal set for 02:00 PM. VIP dining lounge configured.',
      status: 'Pending Setup',
    },
  ];

  const currentEvent = eventList[selectedEventIndex];

  return (
    <Box
      sx={{
        display: 'flex',
        minHeight: '100vh',
        backgroundColor: '#F7F3E9',
        p: { xs: 1.5, md: 2.5 },
        gap: { xs: 2, md: 3 },
      }}
    >
      {/* 1. Left Collapsible Dark Obsidian Sidebar */}
      <CollapsibleSidebar
        activeTab={activeTab}
        onSelectTab={setActiveTab}
        onLogout={handleLogout}
      />

      {/* 2. Central Feed & Content Area */}
      <Box sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        {/* Top Search & Action Bar */}
        <TopSearchNavbar user={user} onSearch={() => {}} />

        {/* Hero Greeting Section */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', mb: 3 }}>
          <Box>
            <Typography
              variant="h4"
              sx={{
                fontWeight: 700,
                color: '#1E1E22',
                letterSpacing: '-0.02em',
                fontFamily: '"Plus Jakarta Sans", sans-serif',
              }}
            >
              Good morning, {displayName.charAt(0).toUpperCase() + displayName.slice(1)}
            </Typography>
            <Typography variant="body2" sx={{ color: '#636369', mt: 0.5 }}>
              Plan It wishes you a good and productive day. 45 bookings waiting for your confirmation today. You also have one live event in your calendar.
            </Typography>
          </Box>

          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Button
              variant="text"
              onClick={() => navigate('/test')}
              sx={{
                color: '#1E1E22',
                fontWeight: 600,
                fontSize: '13px',
                textDecoration: 'underline',
                textUnderlineOffset: '4px',
                p: 0,
                minWidth: 'auto',
                '&:hover': { background: 'none', color: '#000' },
              }}
            >
              Show all &rarr;
            </Button>
          </Box>
        </Box>

        {/* Four Pastel Metric Cards Grid */}
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', xl: 'repeat(4, 1fr)' },
            gap: 2.5,
            mb: 3.5,
          }}
        >
          {/* Yellow Card: Bookings & Mini Bar Chart */}
          <PastelStatCard
            variant="yellow"
            title="Bookings:"
            watermark={
              <svg width="60" height="60" viewBox="0 0 24 24" fill="currentColor">
                <rect x="2" y="2" width="20" height="20" rx="6" />
              </svg>
            }
          >
            <Box sx={{ display: 'flex', gap: 2.5, mb: 2 }}>
              <Box>
                <Typography sx={{ fontSize: '15px', fontWeight: 700, color: '#3E340B' }}>14 pers</Typography>
                <Typography sx={{ fontSize: '10px', color: '#6D5B18' }}>20-30 YO</Typography>
              </Box>
              <Box>
                <Typography sx={{ fontSize: '15px', fontWeight: 700, color: '#3E340B' }}>5 pers</Typography>
                <Typography sx={{ fontSize: '10px', color: '#6D5B18' }}>30-40 YO</Typography>
              </Box>
              <Box>
                <Typography sx={{ fontSize: '15px', fontWeight: 700, color: '#3E340B' }}>2 pers</Typography>
                <Typography sx={{ fontSize: '10px', color: '#6D5B18' }}>40+ YO</Typography>
              </Box>
            </Box>

            {/* Mini Bar Chart Graphic */}
            <Box sx={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', height: 44, pt: 1 }}>
              {[18, 32, 12, 42, 28, 14, 22].map((h, i) => (
                <Box
                  key={i}
                  sx={{
                    width: 7,
                    height: `${h}px`,
                    backgroundColor: i === 3 ? '#19191C' : '#E8CA65',
                    borderRadius: 9999,
                  }}
                />
              ))}
            </Box>
          </PastelStatCard>

          {/* Pink Card: Visits & Budget Summary with Sparkline */}
          <PastelStatCard
            variant="pink"
            title="Visits summary:"
            watermark={
              <svg width="64" height="64" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z" />
              </svg>
            }
          >
            <Box sx={{ display: 'flex', gap: 2.5, mb: 1.5 }}>
              <Box>
                <Typography sx={{ fontSize: '15px', fontWeight: 700, color: '#481931' }}>24 min</Typography>
                <Typography sx={{ fontSize: '9.5px', color: '#7A2C55', textTransform: 'uppercase' }}>Average</Typography>
              </Box>
              <Box>
                <Typography sx={{ fontSize: '15px', fontWeight: 700, color: '#481931' }}>15 min</Typography>
                <Typography sx={{ fontSize: '9.5px', color: '#7A2C55', textTransform: 'uppercase' }}>Minimum</Typography>
              </Box>
              <Box>
                <Typography sx={{ fontSize: '15px', fontWeight: 700, color: '#481931' }}>01:30 h</Typography>
                <Typography sx={{ fontSize: '9.5px', color: '#7A2C55', textTransform: 'uppercase' }}>Maximum</Typography>
              </Box>
            </Box>

            {/* Sparkline Wave */}
            <Box sx={{ position: 'relative', height: 42, width: '100%' }}>
              <svg viewBox="0 0 200 40" style={{ width: '100%', height: '100%', overflow: 'visible' }}>
                <path
                  d="M0,28 Q25,32 50,22 T100,18 T150,5 T200,24"
                  fill="none"
                  stroke="#8B2C5C"
                  strokeWidth="2.2"
                />
                <circle cx="150" cy="5" r="4.5" fill="#19191C" />
              </svg>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 0.5 }}>
                <Typography sx={{ fontSize: '8.5px', color: '#7A2C55' }}>10:30</Typography>
                <Typography sx={{ fontSize: '8.5px', color: '#7A2C55' }}>12:00</Typography>
                <Typography sx={{ fontSize: '8.5px', color: '#7A2C55' }}>13:30</Typography>
              </Box>
            </Box>
          </PastelStatCard>

          {/* Green Card: By Category / Condition */}
          <PastelStatCard
            variant="green"
            title="By category:"
            watermark={
              <svg width="60" height="60" viewBox="0 0 24 24" fill="currentColor">
                <polygon points="12 2 2 22 22 22" />
              </svg>
            }
          >
            <Box sx={{ display: 'flex', gap: 2.5, mb: 3 }}>
              <Box>
                <Typography sx={{ fontSize: '15px', fontWeight: 700, color: '#20361A' }}>14 pers</Typography>
                <Typography sx={{ fontSize: '9.5px', color: '#42613B' }}>STABLE</Typography>
              </Box>
              <Box>
                <Typography sx={{ fontSize: '15px', fontWeight: 700, color: '#20361A' }}>5 pers</Typography>
                <Typography sx={{ fontSize: '9.5px', color: '#42613B' }}>MEDIUM</Typography>
              </Box>
              <Box>
                <Typography sx={{ fontSize: '15px', fontWeight: 700, color: '#20361A' }}>1 pers</Typography>
                <Typography sx={{ fontSize: '9.5px', color: '#42613B' }}>CRITICAL</Typography>
              </Box>
            </Box>

            <Box sx={{ backgroundColor: 'rgba(255,255,255,0.4)', borderRadius: 9999, p: '4px 10px', width: 'fit-content' }}>
              <Typography sx={{ fontSize: '10.5px', fontWeight: 600, color: '#20361A' }}>
                92% Health Index
              </Typography>
            </Box>
          </PastelStatCard>

          {/* Blue Card: Sessions / Vendor Meetings */}
          <PastelStatCard
            variant="blue"
            title="Sessions:"
            watermark={
              <svg width="60" height="60" viewBox="0 0 24 24" fill="currentColor">
                <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2" />
              </svg>
            }
          >
            <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
              <Box>
                <Typography sx={{ fontSize: '14px', fontWeight: 700, color: '#153251' }}>03:45 h</Typography>
                <Typography sx={{ fontSize: '9px', color: '#375B82' }}>IN CLINIC</Typography>
              </Box>
              <Box>
                <Typography sx={{ fontSize: '14px', fontWeight: 700, color: '#153251' }}>02:00 min</Typography>
                <Typography sx={{ fontSize: '9px', color: '#375B82' }}>VIDEO CALLS</Typography>
              </Box>
              <Box>
                <Typography sx={{ fontSize: '14px', fontWeight: 700, color: '#153251' }}>00:24 min</Typography>
                <Typography sx={{ fontSize: '9px', color: '#375B82' }}>IN CHAT</Typography>
              </Box>
            </Box>

            <Box sx={{ display: 'flex', gap: 1 }}>
              <Chip label="Clinic" size="small" sx={{ height: 22, fontSize: '10px', bgcolor: 'rgba(255,255,255,0.6)' }} />
              <Chip label="Online" size="small" sx={{ height: 22, fontSize: '10px', bgcolor: 'rgba(255,255,255,0.6)' }} />
            </Box>
          </PastelStatCard>
        </Box>

        {/* Lower Grid: Patient / Event List (Left) + Detail Card (Right) */}
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' },
            gap: 2.5,
            flex: 1,
          }}
        >
          {/* Left Column: Event / Patient List */}
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="h6" sx={{ fontWeight: 700, fontSize: '1.05rem', color: '#1E1E22' }}>
                Schedule & Event List
              </Typography>
              <Box
                sx={{
                  backgroundColor: '#19191C',
                  color: '#FFFFFF',
                  px: 1.5,
                  py: 0.4,
                  borderRadius: 9999,
                  fontSize: '11px',
                  fontWeight: 600,
                  display: 'flex',
                  alignItems: 'center',
                  gap: 0.5,
                  cursor: 'pointer',
                }}
              >
                Today
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3">
                  <polyline points="6 9 12 15 18 9" />
                </svg>
              </Box>
            </Box>

            {/* List Items */}
            {eventList.map((evt, idx) => {
              const isSelected = selectedEventIndex === idx;
              return (
                <Box
                  key={evt.id}
                  onClick={() => setSelectedEventIndex(idx)}
                  sx={{
                    backgroundColor: isSelected ? '#FFFFFF' : 'rgba(255,255,255,0.6)',
                    borderRadius: '18px',
                    p: '14px 16px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    cursor: 'pointer',
                    boxShadow: isSelected ? '0 4px 18px rgba(35,25,15,0.06)' : 'none',
                    border: isSelected ? '1px solid rgba(0,0,0,0.06)' : '1px solid transparent',
                    transition: 'all 0.15s ease',
                    '&:hover': {
                      backgroundColor: '#FFFFFF',
                      transform: 'translateY(-1px)',
                    },
                  }}
                >
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                    {/* Circle Pastel Badge */}
                    <Box
                      sx={{
                        width: 38,
                        height: 38,
                        borderRadius: '50%',
                        backgroundColor: evt.color,
                        color: evt.textColor,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        fontWeight: 700,
                        fontSize: '13px',
                      }}
                    >
                      {evt.title.charAt(0)}
                    </Box>

                    <Box>
                      <Typography sx={{ fontSize: '13.5px', fontWeight: 600, color: '#1E1E22' }}>
                        {evt.title}
                      </Typography>
                      <Typography sx={{ fontSize: '11.5px', color: '#8F8F96' }}>
                        {evt.category}
                      </Typography>
                    </Box>
                  </Box>

                  {/* Time Pill Badge */}
                  <Box
                    sx={{
                      backgroundColor: isSelected ? '#F9BFD8' : 'rgba(0,0,0,0.04)',
                      color: isSelected ? '#481931' : '#636369',
                      px: 1.5,
                      py: 0.4,
                      borderRadius: 9999,
                      fontSize: '11px',
                      fontWeight: 600,
                    }}
                  >
                    {evt.time}
                  </Box>
                </Box>
              );
            })}
          </Box>

          {/* Right Column: Selected Event / Visit Details */}
          <Box
            sx={{
              backgroundColor: '#FFFFFF',
              borderRadius: '24px',
              p: '22px 20px',
              boxShadow: '0 4px 24px rgba(35,25,15,0.04)',
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'space-between',
            }}
          >
            <Box>
              {/* Header with Title & Code */}
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Box>
                  <Typography sx={{ fontSize: '16px', fontWeight: 700, color: '#1E1E22' }}>
                    {currentEvent.title}
                  </Typography>
                  <Typography sx={{ fontSize: '12px', color: '#8F8F96' }}>
                    Coordinator: {currentEvent.coordinator} • {currentEvent.date}
                  </Typography>
                </Box>
                <Chip
                  label={currentEvent.code}
                  size="small"
                  sx={{
                    borderRadius: 9999,
                    backgroundColor: '#FAF7EF',
                    color: '#636369',
                    fontWeight: 600,
                    fontSize: '11px',
                  }}
                />
              </Box>

              {/* Tag Badges */}
              <Box sx={{ display: 'flex', gap: 1, mb: 3, flexWrap: 'wrap' }}>
                {currentEvent.tags.map((tag, i) => (
                  <Chip
                    key={tag}
                    label={tag}
                    size="small"
                    sx={{
                      borderRadius: 9999,
                      backgroundColor: i === 0 ? '#FDEEF5' : i === 1 ? '#EEF5FC' : '#FEF8E4',
                      color: i === 0 ? '#481931' : i === 1 ? '#153251' : '#3E340B',
                      fontWeight: 600,
                      fontSize: '11px',
                    }}
                  />
                ))}
              </Box>

              {/* Details List */}
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.8 }}>
                <Box>
                  <Typography sx={{ fontSize: '11px', textTransform: 'uppercase', color: '#8F8F96', fontWeight: 600 }}>
                    Operational Status
                  </Typography>
                  <Typography sx={{ fontSize: '13.5px', color: '#1E1E22', fontWeight: 500 }}>
                    {currentEvent.status}
                  </Typography>
                </Box>

                <Box>
                  <Typography sx={{ fontSize: '11px', textTransform: 'uppercase', color: '#8F8F96', fontWeight: 600 }}>
                    Observation & Checklist Notes
                  </Typography>
                  <Typography sx={{ fontSize: '13px', color: '#636369', lineHeight: 1.5 }}>
                    {currentEvent.notes}
                  </Typography>
                </Box>
              </Box>
            </Box>

            {/* Bottom Actions */}
            <Box sx={{ display: 'flex', gap: 1.5, mt: 3, pt: 2, borderTop: '1px solid rgba(0,0,0,0.06)' }}>
              <Button
                variant="contained"
                onClick={() => navigate('/test')}
                sx={{
                  backgroundColor: '#19191C',
                  color: '#FFFFFF',
                  borderRadius: 9999,
                  flex: 1,
                  py: 1,
                  fontSize: '12.5px',
                  fontWeight: 600,
                  '&:hover': { backgroundColor: '#2E2E36' },
                }}
              >
                API Integration Test
              </Button>
              <Button
                variant="outlined"
                onClick={handleLogout}
                sx={{
                  borderColor: 'rgba(0,0,0,0.1)',
                  color: '#1E1E22',
                  borderRadius: 9999,
                  py: 1,
                  fontSize: '12.5px',
                  fontWeight: 600,
                  '&:hover': { borderColor: '#19191C', backgroundColor: 'rgba(0,0,0,0.02)' },
                }}
              >
                Sign Out
              </Button>
            </Box>
          </Box>
        </Box>
      </Box>

      {/* 3. Right Rail: Month Calendar & Today's Timeline Panel */}
      <CalendarTimelineRail
        onAddEvent={() => {}}
        onViewDetails={() => navigate('/test')}
      />
    </Box>
  );
}
