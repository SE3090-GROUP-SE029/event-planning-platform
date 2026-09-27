import { useState } from 'react';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import StorefrontOutlinedIcon from '@mui/icons-material/StorefrontOutlined';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import { resolveVendorImageUrl } from '../../vendors/api/vendorApi';
import { useAdminUserVendors } from '../api/adminDashboardApi';
import { tokens } from '../../../shared/theme/tokens';

export default function AdminVendorsPage() {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const vendorsQuery = useAdminUserVendors({ page: page + 1, pageSize, search });
  const { data, isLoading } = vendorsQuery;
  const vendors = data?.items || [];
  const filteredVendors = vendors;

  return (
    <AppLayout
      activeTab="admin-vendors"
      title="Vendor Oversight"
      subtitle="Review approved and active marketplace suppliers"
      onSearch={setSearch}
    >
      <Box sx={{ mb: 3 }}>
        <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
          Vendor Network
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Monitor marketplace businesses, service offerings, and verification status.
        </Typography>
      </Box>

      <SurfaceCard sx={{ p: 2.5, mb: 3 }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <TextField
            size="small"
            fullWidth
            placeholder="Search by business name, category, or email"
            value={search}
            onChange={(e) => {
              setPage(0);
              setSearch(e.target.value);
            }}
          />
          {search && (
            <Button variant="text" onClick={() => { setPage(0); setSearch(''); }}>
              Clear
            </Button>
          )}
        </Stack>
      </SurfaceCard>

      <SurfaceCard sx={{ p: 3 }}>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
            <CircularProgress />
          </Box>
        ) : vendorsQuery.isError ? (
          <Alert
            severity="error"
            action={<Button color="inherit" size="small" onClick={() => vendorsQuery.refetch()}>Retry</Button>}
          >
            Vendors could not be loaded. {vendorsQuery.error.message}
          </Alert>
        ) : filteredVendors.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: 6 }}>
            <Typography color="text.secondary">No vendors found matching your search.</Typography>
          </Box>
        ) : (
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Business</TableCell>
                  <TableCell>Category</TableCell>
                  <TableCell>Contact</TableCell>
                  <TableCell>Address</TableCell>
                  <TableCell>Status</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filteredVendors.map((v) => {
                  const logo = resolveVendorImageUrl(v.profileImageUrl);
                  return (
                    <TableRow hover key={v.id}>
                      <TableCell>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                          <Avatar src={logo || undefined} sx={{ width: 38, height: 38, bgcolor: tokens.colors.pastelPinkLight, color: tokens.colors.pastelPinkText }}>
                            {!logo && <StorefrontOutlinedIcon fontSize="small" />}
                          </Avatar>
                          <Typography sx={{ fontWeight: 700 }}>{v.businessName}</Typography>
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          label={v.category || 'General'}
                          sx={{ bgcolor: tokens.colors.pastelBlueLight, color: tokens.colors.pastelBlueText, fontWeight: 700 }}
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">{v.contactEmail || '—'}</Typography>
                        <Typography variant="caption" color="text.secondary">{v.contactPhone || '—'}</Typography>
                      </TableCell>
                      <TableCell sx={{ maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {v.address || '—'}
                      </TableCell>
                      <TableCell>
                        <StatusBadge status={v.status} label={v.status} />
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        )}
        {!isLoading && !vendorsQuery.isError && (
          <TablePagination
            component="div"
            count={data?.totalCount ?? vendors.length}
            page={page}
            rowsPerPage={pageSize}
            onPageChange={(_, nextPage) => setPage(nextPage)}
            onRowsPerPageChange={(event) => {
              setPage(0);
              setPageSize(Number(event.target.value));
            }}
            rowsPerPageOptions={[10, 25, 50, 100]}
          />
        )}
      </SurfaceCard>
    </AppLayout>
  );
}
