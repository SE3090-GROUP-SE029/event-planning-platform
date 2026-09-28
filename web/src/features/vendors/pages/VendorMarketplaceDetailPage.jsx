import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Avatar,
  Box,
  Button,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material';
import StorefrontOutlinedIcon from '@mui/icons-material/StorefrontOutlined';
import { useAuthStore } from '../../../shared/store/authStore';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import {
  resolveVendorImageUrl,
  useVendorMarketplaceDetail,
} from '../api/vendorApi';

function formatPrice(price, pricingType) {
  if (price == null) return 'Price on request';
  const amount = `Rs. ${Number(price).toLocaleString('en-LK', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  })}`;
  const labels = {
    FIXED: 'Fixed',
    PER_PERSON: 'Per person',
    PER_HOUR: 'Per hour',
    PER_DAY: 'Per day',
  };
  if (!pricingType) return amount;
  return `${amount} — ${labels[pricingType] || pricingType}`;
}

function formatPeriod(iso) {
  return new Date(iso).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

export default function VendorMarketplaceDetailPage() {
  const { vendorId } = useParams();
  const navigate = useNavigate();
  const isEventPlanner = useAuthStore((state) => state.hasRole('EVENT_PLANNER'));
  const { data: vendor, isLoading, isError, error } = useVendorMarketplaceDetail(vendorId);
  const imageUrl = resolveVendorImageUrl(vendor?.profileImageUrl);

  return (
    <AppLayout
      activeTab="marketplace"
      title="Vendor details"
      subtitle="Read-only marketplace profile"
    >
      <Box sx={{ maxWidth: 1200, mx: 'auto', mt: 1 }}>
        <Button variant="text" sx={{ mb: 2 }} onClick={() => navigate('/marketplace')}>
          ← Back to marketplace
        </Button>

          {isLoading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
              <CircularProgress />
            </Box>
          )}

          {isError && (
            <Alert severity="error" sx={{ borderRadius: 3 }}>
              {error?.message || 'Vendor not found.'}
            </Alert>
          )}

          {vendor && (
            <Stack spacing={2} sx={{ maxWidth: 920 }}>
              <SurfaceCard sx={{ p: 3 }}>
                <Box sx={{ display: 'flex', gap: 2.5, flexWrap: 'wrap', alignItems: 'center' }}>
                  <Avatar
                    src={imageUrl || undefined}
                    sx={{ width: 96, height: 96, bgcolor: '#E8E4DA', color: '#19191C' }}
                  >
                    {!imageUrl && <StorefrontOutlinedIcon />}
                  </Avatar>
                  <Box sx={{ flex: 1, minWidth: 240 }}>
                    <Typography variant="h4" sx={{ fontWeight: 800, letterSpacing: '-0.04em' }}>
                      {vendor.businessName}
                    </Typography>
                    <Typography color="text.secondary">{vendor.category}</Typography>
                    <Typography sx={{ mt: 1.5 }}>{vendor.description || 'No description provided.'}</Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                      {vendor.address}
                    </Typography>
                    {vendor.websiteUrl && (
                      <Typography variant="body2" sx={{ mt: 0.5 }}>
                        <a href={vendor.websiteUrl} target="_blank" rel="noreferrer">
                          {vendor.websiteUrl}
                        </a>
                      </Typography>
                    )}
                    {isEventPlanner && (
                      <Button
                        variant="contained"
                        sx={{ mt: 2, bgcolor: '#19191C' }}
                        onClick={() => navigate(`/marketplace/${vendorId}/request-quotation`)}
                        disabled={!vendor.services?.length}
                      >
                        Request quotation
                      </Button>
                    )}
                  </Box>
                </Box>
              </SurfaceCard>

              <SurfaceCard sx={{ p: 3 }}>
                <Typography variant="h6" sx={{ fontWeight: 700, mb: 1.5 }}>
                  Business photos
                </Typography>
                {vendor.images?.length ? (
                  <Box sx={{ display: 'flex', gap: 1.5, flexWrap: 'wrap' }}>
                    {vendor.images.map((image) => (
                      <Box
                        key={image.id}
                        component="img"
                        src={resolveVendorImageUrl(image.imageUrl) || undefined}
                        alt={`${vendor.businessName} gallery`}
                        sx={{
                          width: 140,
                          height: 110,
                          objectFit: 'cover',
                          borderRadius: 2,
                          bgcolor: '#E8E4DA',
                        }}
                      />
                    ))}
                  </Box>
                ) : (
                  <Typography color="text.secondary">No gallery photos yet.</Typography>
                )}
              </SurfaceCard>

              <SurfaceCard sx={{ p: 3 }}>
                <Typography variant="h6" sx={{ fontWeight: 700, mb: 1.5 }}>
                  Services
                </Typography>
                <Stack spacing={1.5}>
                  {(vendor.services || []).length === 0 && (
                    <Typography color="text.secondary">No services listed.</Typography>
                  )}
                  {(vendor.services || []).map((service) => (
                    <Box key={service.id} sx={{ borderBottom: '1px solid #E8E4DA', pb: 1.5 }}>
                      <Typography sx={{ fontWeight: 700 }}>{service.serviceName}</Typography>
                      <Typography variant="body2" color="text.secondary">
                        {service.description || 'No description'}
                      </Typography>
                      <Typography sx={{ mt: 0.5, fontWeight: 700 }}>
                        {formatPrice(service.price, service.pricingType)}
                      </Typography>
                    </Box>
                  ))}
                </Stack>
              </SurfaceCard>

              <SurfaceCard sx={{ p: 3 }}>
                <Typography variant="h6" sx={{ fontWeight: 700, mb: 1.5 }}>
                  Upcoming availability
                </Typography>
                <Stack spacing={1}>
                  {(vendor.availability || []).length === 0 && (
                    <Typography color="text.secondary">No upcoming availability listed.</Typography>
                  )}
                  {(vendor.availability || []).map((period) => (
                    <Typography key={period.id} variant="body2">
                      {period.isAvailable ? 'Available' : 'Unavailable'}: {formatPeriod(period.startDateTime)} →{' '}
                      {formatPeriod(period.endDateTime)}
                    </Typography>
                  ))}
                </Stack>
              </SurfaceCard>
            </Stack>
          )}
        </Box>
    </AppLayout>
  );
}
