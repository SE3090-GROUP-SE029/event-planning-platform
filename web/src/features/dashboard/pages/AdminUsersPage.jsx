import { useState } from 'react';
import {
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Alert,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
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
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import { useAdminUsers } from '../api/adminDashboardApi';
import { tokens } from '../../../shared/theme/tokens';

export default function AdminUsersPage() {
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('ALL');
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const usersQuery = useAdminUsers({
    page: page + 1,
    pageSize,
    search,
    role: roleFilter === 'ALL' ? undefined : roleFilter,
  });
  const users = usersQuery.data?.items || [];
  const filteredUsers = users;

  const isLoading = usersQuery.isLoading;

  return (
    <AppLayout
      activeTab="users"
      title="User Management"
      subtitle="Inspect and manage all platform accounts"
      onSearch={setSearch}
    >
      <Box sx={{ mb: 3 }}>
        <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
          Platform Users
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Monitor registered event planners, verified vendors, and administrative personnel.
        </Typography>
      </Box>

      {/* Filter Surface */}
      <SurfaceCard sx={{ p: 2.5, mb: 3 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: 'center' }}>
          <TextField
            size="small"
            placeholder="Filter by name or email"
            value={search}
            onChange={(e) => {
              setPage(0);
              setSearch(e.target.value);
            }}
            sx={{ flex: 1 }}
          />
          <FormControl size="small" sx={{ minWidth: 200 }}>
            <InputLabel>Filter by Role</InputLabel>
            <Select
              value={roleFilter}
              label="Filter by Role"
              onChange={(e) => {
                setPage(0);
                setRoleFilter(e.target.value);
              }}
            >
              <MenuItem value="ALL">
                All Roles ({usersQuery.data?.totalCount ?? users.length})
              </MenuItem>
              <MenuItem value="EVENT_PLANNER">Event Planners</MenuItem>
              <MenuItem value="VENDOR">Vendors</MenuItem>
              <MenuItem value="ADMIN">Administrators</MenuItem>
            </Select>
          </FormControl>
          <Button
            variant="text"
            onClick={() => {
              setSearch('');
              setRoleFilter('ALL');
              setPage(0);
            }}
          >
            Clear Filters
          </Button>
        </Stack>
      </SurfaceCard>

      {/* Users Table */}
      <SurfaceCard sx={{ p: 3 }}>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
            <CircularProgress />
          </Box>
        ) : usersQuery.isError ? (
          <Alert
            severity="error"
            action={<Button color="inherit" size="small" onClick={() => usersQuery.refetch()}>Retry</Button>}
          >
            Users could not be loaded. {usersQuery.error.message}
          </Alert>
        ) : filteredUsers.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: 6 }}>
            <Typography color="text.secondary">No users found matching your filters.</Typography>
          </Box>
        ) : (
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>User</TableCell>
                  <TableCell>Role</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Identifier</TableCell>
                  <TableCell>Registration</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filteredUsers.map((u) => {
                  const role = u.roles[0] || 'USER';
                  const roleColor =
                    role === 'ADMIN'
                      ? { bg: tokens.colors.pastelYellowLight, text: tokens.colors.pastelYellowText }
                      : role === 'VENDOR'
                      ? { bg: tokens.colors.pastelPinkLight, text: tokens.colors.pastelPinkText }
                      : { bg: tokens.colors.pastelBlueLight, text: tokens.colors.pastelBlueText };

                  return (
                    <TableRow hover key={u.id}>
                      <TableCell>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                          <Avatar sx={{ width: 36, height: 36, bgcolor: tokens.colors.pastelBlue, color: tokens.colors.pastelBlueText, fontWeight: 800 }}>
                            {u.email.charAt(0).toUpperCase()}
                          </Avatar>
                          <Box>
                            <Typography sx={{ fontWeight: 700 }}>{`${u.firstName || ''} ${u.lastName || ''}`.trim() || '—'}</Typography>
                            <Typography variant="body2" color="text.secondary">{u.email}</Typography>
                          </Box>
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          label={role.replace('_', ' ')}
                          sx={{ bgcolor: roleColor.bg, color: roleColor.text, fontWeight: 700, fontSize: '0.72rem' }}
                        />
                      </TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          label={u.isActive ? 'ACTIVE' : 'INACTIVE'}
                          sx={{
                            bgcolor: u.isActive ? tokens.colors.pastelGreenLight : tokens.colors.surfaceMuted,
                            color: u.isActive ? tokens.colors.pastelGreenText : tokens.colors.textSecondary,
                            fontWeight: 700,
                            fontSize: '0.72rem',
                          }}
                        />
                      </TableCell>
                      <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.8rem', color: tokens.colors.textSecondary }}>
                        {String(u.id).slice(0, 18)}…
                      </TableCell>
                      <TableCell>{u.createdAt ? new Date(u.createdAt).toLocaleDateString() : '—'}</TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        )}
        {!isLoading && !usersQuery.isError && (
          <TablePagination
            component="div"
            count={usersQuery.data?.totalCount ?? users.length}
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
