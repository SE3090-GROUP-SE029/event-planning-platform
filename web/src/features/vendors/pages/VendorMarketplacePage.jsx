import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Avatar,
  Box,
  Button,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Pagination,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import StorefrontOutlinedIcon from '@mui/icons-material/StorefrontOutlined';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import {
  resolveVendorImageUrl,
  useVendorMarketplace,
  VENDOR_CATEGORIES,
} from '../api/vendorApi';

function formatStartingPrice(price, pricingType) {
  if (price == null) return 'Price on request';
  const amount = `Rs. ${Number(price).toLocaleString('en-LK', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  })}`;
  if (!pricingType) return amount;
  const labels = {
    FIXED: 'Fixed',
    PER_PERSON: 'Per person',
    PER_HOUR: 'Per hour',
    PER_DAY: 'Per day',
  };
  return `${amount} — ${labels[pricingType] || pricingType}`;
}

export default function VendorMarketplacePage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const isVendor = useAuthStore((state) => state.hasRole('VENDOR'));
  const isAdmin = useAuthStore((state) => state.hasRole('ADMIN'));

  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [sortBy, setSortBy] = useState('businessName');
  const [sortOrder, setSortOrder] = useState('asc');
  const [page, setPage] = useState(1);
  const pageSize = 6;

  const query = useMemo(
    () => ({ search, category, sortBy, sortOrder, page, pageSize }),
    [search, category, sortBy, sortOrder, page, pageSize],
  );

  const { data, isLoading, isError, error } = useVendorMarketplace(query);
  const items = data?.items ?? [];
  const totalPages = data?.totalPages || 0;

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 } }}>
      <Box sx={{ display: 'flex', gap: { xs: 2, md: 3 }, minHeight: 'calc(100vh - 32px)' }}>
        <CollapsibleSidebar
          activeTab="marketplace"
          showEvents={isAdmin}
          showMarketplace
          showVendorProfile={isVendor}
          showVendorServices={isVendor}
          showVendorAvailability={isVendor}
          onSelectTab={(tab) => {
            if (tab === 'dashboard') navigate('/dashboard');
            if (tab === 'events') navigate('/admin/events');
            if (tab === 'marketplace') navigate('/marketplace');
            if (tab === 'vendor-profile') navigate('/vendor/profile');
            if (tab === 'vendor-services') navigate('/vendor/services');
            if (tab === 'vendor-availability') navigate('/vendor/availability');
          }}
          onLogout={handleLogout}
        />

        <Box sx={{ flex: 1, minWidth: 0 }}>
          <TopSearchNavbar user={user} title="Vendor Marketplace" subtitle="Discover approved vendors for your events" />

          <SurfaceCard sx={{ p: 2.5, mb: 3 }}>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
              <TextField
                fullWidth
                size="small"
                label="Search vendors"
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter') {
                    setPage(1);
                    setSearch(searchInput.trim());
                  }
                }}
              />
              <FormControl size="small" sx={{ minWidth: 180 }}>
                <InputLabel id="marketplace-category">Category</InputLabel>
                <Select
                  labelId="marketplace-category"
                  label="Category"
                  value={category}
                  onChange={(event) => {
                    setPage(1);
                    setCategory(event.target.value);
                  }}
                >
                  <MenuItem value="">All</MenuItem>
                  {VENDOR_CATEGORIES.map((item) => (
                    <MenuItem key={item} value={item}>
                      {item}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl size="small" sx={{ minWidth: 180 }}>
                <InputLabel id="marketplace-sort">Sort by</InputLabel>
                <Select
                  labelId="marketplace-sort"
                  label="Sort by"
                  value={sortBy}
                  onChange={(event) => {
                    setPage(1);
                    setSortBy(event.target.value);
                  }}
                >
                  <MenuItem value="businessName">Business name</MenuItem>
                  <MenuItem value="category">Category</MenuItem>
                  <MenuItem value="createdAt">Newest</MenuItem>
                  <MenuItem value="startingPrice">Starting price</MenuItem>
                </Select>
              </FormControl>
              <FormControl size="small" sx={{ minWidth: 120 }}>
                <InputLabel id="marketplace-order">Order</InputLabel>
                <Select
                  labelId="marketplace-order"
                  label="Order"
                  value={sortOrder}
                  onChange={(event) => {
                    setPage(1);
                    setSortOrder(event.target.value);
                  }}
                >
                  <MenuItem value="asc">Asc</MenuItem>
                  <MenuItem value="desc">Desc</MenuItem>
                </Select>
              </FormControl>
              <Button
                variant="contained"
                sx={{ borderRadius: 9999, whiteSpace: 'nowrap' }}
                onClick={() => {
                  setPage(1);
                  setSearch(searchInput.trim());
                }}
              >
                Search
              </Button>
            </Stack>
          </SurfaceCard>

          {isError && (
            <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
              {error?.message || 'Unable to load marketplace vendors.'}
            </Alert>
          )}

          {isLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
              <CircularProgress />
            </Box>
          ) : items.length === 0 ? (
            <SurfaceCard sx={{ p: 3 }}>
              <Typography color="text.secondary">No approved vendors match your filters.</Typography>
            </SurfaceCard>
          ) : (
            <Stack spacing={1.5}>
              {items.map((vendor) => {
                const imageUrl = resolveVendorImageUrl(vendor.profileImageUrl);
                return (
                  <SurfaceCard key={vendor.id} sx={{ p: 2.5 }}>
                    <Box sx={{ display: 'flex', gap: 2, alignItems: 'flex-start', flexWrap: 'wrap' }}>
                      <Avatar
                        src={imageUrl || undefined}
                        sx={{ width: 72, height: 72, bgcolor: '#E8E4DA', color: '#19191C' }}
                      >
                        {!imageUrl && <StorefrontOutlinedIcon />}
                      </Avatar>
                      <Box sx={{ flex: 1, minWidth: 220 }}>
                        <Typography variant="h6" sx={{ fontWeight: 800 }}>
                          {vendor.businessName}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {vendor.category}
                        </Typography>
                        <Typography variant="body2" sx={{ mt: 0.75 }}>
                          {vendor.shortDescription || 'No description provided.'}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                          {vendor.address}
                        </Typography>
                        <Typography variant="body2" sx={{ mt: 1, fontWeight: 700 }}>
                          {formatStartingPrice(vendor.startingPrice, vendor.startingPricingType)}
                        </Typography>
                      </Box>
                      <Button
                        variant="contained"
                        sx={{ borderRadius: 9999 }}
                        onClick={() => navigate(`/marketplace/${vendor.id}`)}
                      >
                        View Vendor
                      </Button>
                    </Box>
                  </SurfaceCard>
                );
              })}
            </Stack>
          )}

          {totalPages > 1 && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
              <Pagination
                count={totalPages}
                page={page}
                onChange={(_, value) => setPage(value)}
                color="primary"
              />
            </Box>
          )}
        </Box>
      </Box>
    </Box>
  );
}
