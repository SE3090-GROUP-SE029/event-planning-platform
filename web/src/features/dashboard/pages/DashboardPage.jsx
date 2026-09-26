import React, { useState } from 'react';
import { Avatar, Box, Button, CircularProgress, Stack, TextField, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import StorefrontOutlinedIcon from '@mui/icons-material/StorefrontOutlined';
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import {
  resolveVendorImageUrl,
  useMyVendorGallery,
  useMyVendorProfile,
  useMyVendorServices,
} from '../../vendors/api/vendorApi';

function VendorDashboardOverview() {
  const navigate = useNavigate();
  const { data: profile, isLoading: profileLoading } = useMyVendorProfile();
  const hasProfile = Boolean(profile);
  const { data: services = [], isLoading: servicesLoading } = useMyVendorServices(hasProfile);
  const { data: gallery = [], isLoading: galleryLoading } = useMyVendorGallery(hasProfile);
  const imageUrl = resolveVendorImageUrl(profile?.profileImageUrl);
  const recentServices = services.slice(0, 3);

  if (profileLoading || (hasProfile && (servicesLoading || galleryLoading))) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!hasProfile) {
    return (
      <SurfaceCard sx={{ p: 3, maxWidth: 720 }}>
        <Typography variant="h5" sx={{ fontWeight: 800, mb: 1 }}>
          Set up your vendor profile
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 2 }}>
          Create your business profile to start adding services to the marketplace.
        </Typography>
        <Button variant="contained" sx={{ borderRadius: 9999 }} onClick={() => navigate('/vendor/profile')}>
          Create profile
        </Button>
      </SurfaceCard>
    );
  }

  return (
    <Stack spacing={2.5} sx={{ maxWidth: 920 }}>
      <SurfaceCard sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', gap: 2.5, flexWrap: 'wrap', alignItems: 'center' }}>
          <Avatar
            src={imageUrl || undefined}
            sx={{ width: 88, height: 88, bgcolor: '#E8E4DA', color: '#19191C' }}
          >
            {!imageUrl && <StorefrontOutlinedIcon />}
          </Avatar>
          <Box sx={{ flex: 1, minWidth: 220 }}>
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', flexWrap: 'wrap', mb: 0.5 }}>
              <Typography variant="h4" sx={{ fontWeight: 800, letterSpacing: '-0.04em' }}>
                {profile.businessName}
              </Typography>
              <StatusBadge status={profile.status} label={profile.status} />
            </Box>
            <Typography color="text.secondary">{profile.category}</Typography>
            <Typography variant="body2" sx={{ mt: 1 }}>
              {profile.contactEmail} · {profile.contactPhone}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {profile.address}
            </Typography>
            {profile.websiteUrl && (
              <Typography variant="body2" sx={{ mt: 0.5 }}>
                {profile.websiteUrl}
              </Typography>
            )}
          </Box>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
            <Button variant="contained" sx={{ borderRadius: 9999 }} onClick={() => navigate('/vendor/profile')}>
              Edit Profile
            </Button>
            <Button variant="outlined" sx={{ borderRadius: 9999 }} onClick={() => navigate('/vendor/services')}>
              Manage Services
            </Button>
          </Stack>
        </Box>
      </SurfaceCard>

      <SurfaceCard sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, alignItems: 'center', mb: 2, flexWrap: 'wrap' }}>
          <Box>
            <Typography variant="h5" sx={{ fontWeight: 800 }}>
              Business images
            </Typography>
            <Typography color="text.secondary">
              {gallery.length} photo{gallery.length === 1 ? '' : 's'} on your vendor profile
            </Typography>
          </Box>
          <Button variant="outlined" sx={{ borderRadius: 9999 }} onClick={() => navigate('/vendor/profile')}>
            Manage images
          </Button>
        </Box>

        {gallery.length === 0 ? (
          <Typography color="text.secondary">
            No business images yet. Add photos from Edit Profile.
          </Typography>
        ) : (
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(3, 1fr)' },
              gap: 1.5,
            }}
          >
            {gallery.map((image) => {
              const src = resolveVendorImageUrl(image.imageUrl);
              return (
                <SurfaceCard
                  key={image.id}
                  sx={{
                    p: 0,
                    overflow: 'hidden',
                    aspectRatio: '4 / 3',
                  }}
                >
                  <Box
                    component="img"
                    src={src || undefined}
                    alt="Vendor business"
                    sx={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
                  />
                </SurfaceCard>
              );
            })}
          </Box>
        )}
      </SurfaceCard>

      <SurfaceCard sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, alignItems: 'center', mb: 2, flexWrap: 'wrap' }}>
          <Box>
            <Typography variant="h5" sx={{ fontWeight: 800 }}>
              Services summary
            </Typography>
            <Typography color="text.secondary">{services.length} service{services.length === 1 ? '' : 's'} listed</Typography>
          </Box>
          <Button variant="outlined" sx={{ borderRadius: 9999 }} onClick={() => navigate('/vendor/services')}>
            Manage Services
          </Button>
        </Box>

        {recentServices.length === 0 ? (
          <Typography color="text.secondary">No services yet. Add your first service to get started.</Typography>
        ) : (
          <Stack spacing={1.25}>
            {recentServices.map((service) => (
              <Box
                key={service.id}
                sx={{
                  p: 1.75,
                  borderRadius: 3,
                  backgroundColor: '#F7F3E9',
                }}
              >
                <Typography sx={{ fontWeight: 700 }}>{service.serviceName}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {service.description || 'No description'}
                </Typography>
              </Box>
            ))}
          </Stack>
        )}
      </SurfaceCard>
    </Stack>
  );
}

export default function DashboardPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const roles = user?.roles || [];
  const isAdmin = roles.includes('ADMIN');
  const isVendor = roles.includes('VENDOR');
  const isEventPlanner = roles.includes('EVENT_PLANNER');
  const showMarketplace = isEventPlanner || isAdmin;
  const [scheduleEventId, setScheduleEventId] = useState('');

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const handleOpenSchedule = (e) => {
    e.preventDefault();
    if (scheduleEventId.trim()) {
      navigate(`/events/${scheduleEventId.trim()}/schedule`);
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 } }}>
      <Box sx={{ display: 'flex', gap: { xs: 2, md: 3 }, minHeight: 'calc(100vh - 32px)' }}>
        <CollapsibleSidebar
          activeTab="dashboard"
          showEvents={isAdmin}
          showSchedule={true}
          showMarketplace={showMarketplace}
          showMyQuotations={isEventPlanner}
          showVendorProfile={isVendor}
          showVendorServices={isVendor}
          showVendorAvailability={isVendor}
          showVendorQuotations={isVendor}
          onSelectTab={(tab) => {
            if (tab === 'dashboard') navigate('/dashboard');
            if (tab === 'events') navigate('/admin/events');
            if (tab === 'schedule') {
              const eventId = prompt('Enter Event ID to open schedule:');
              if (eventId?.trim()) navigate(`/events/${eventId.trim()}/schedule`);
            }
            if (tab === 'marketplace') navigate('/marketplace');
            if (tab === 'my-quotations') navigate('/quotations/mine');
            if (tab === 'vendor-profile') navigate('/vendor/profile');
            if (tab === 'vendor-services') navigate('/vendor/services');
            if (tab === 'vendor-availability') navigate('/vendor/availability');
            if (tab === 'vendor-quotations') navigate('/vendor/quotations');
          }}
          onLogout={handleLogout}
        />

        <Box sx={{ flex: 1, minWidth: 0 }}>
          <TopSearchNavbar user={user} title="Welcome back" subtitle={user?.email || 'Workspace'} />

          <Box sx={{ mb: 3 }}>
            <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
              {user?.email ? `Hello, ${user.email.split('@')[0]}` : 'Hello'}
            </Typography>
            <Typography variant="body1" color="text.secondary">
              {isVendor
                ? 'Review your vendor profile and services from your marketplace workspace.'
                : 'Use the available workspace tools to manage your active event planning tasks.'}
            </Typography>
          </Box>

          {!isVendor && (
            <SurfaceCard sx={{ p: 3, maxWidth: 580, mb: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1 }}>
                <CalendarMonthIcon sx={{ color: '#19191C' }} />
                <Typography variant="h5" sx={{ fontWeight: 800 }}>
                  Event Schedule Builder
                </Typography>
              </Box>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Enter an Event ID to view its timeline, detect conflicts, and manage activities.
              </Typography>
              <Box component="form" onSubmit={handleOpenSchedule} sx={{ display: 'flex', gap: 1.5 }}>
                <TextField
                  size="small"
                  fullWidth
                  placeholder="Enter Event ID (e.g. GUID)"
                  value={scheduleEventId}
                  onChange={(e) => setScheduleEventId(e.target.value)}
                  sx={{ bgcolor: '#FFFFFF', borderRadius: 2 }}
                />
                <Button variant="contained" type="submit" sx={{ borderRadius: 9999, px: 3, whiteSpace: 'nowrap' }}>
                  Open Schedule
                </Button>
              </Box>
            </SurfaceCard>
          )}

          {isVendor && <VendorDashboardOverview />}
        </Box>
      </Box>
    </Box>
  );
}