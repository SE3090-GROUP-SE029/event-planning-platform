import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material';
import RefreshRoundedIcon from '@mui/icons-material/RefreshRounded';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import { useAdminHealth } from '../api/adminDashboardApi';

export default function AdminSystemPage() {
  const query = useAdminHealth();
  const health = query.data;

  return (
    <AppLayout
      activeTab="system"
      title="System Monitoring"
      subtitle="API availability and database connectivity"
    >
      <Box sx={{ mb: 3 }}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' } }}
        >
          <Box>
            <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
              System Telemetry
            </Typography>
            <Typography variant="body1" color="text.secondary">
              API availability and database connectivity from the administrator health endpoint.
            </Typography>
          </Box>
          <Button
            variant="outlined"
            startIcon={query.isFetching ? <CircularProgress size={16} /> : <RefreshRoundedIcon />}
            sx={{ borderRadius: 9999, px: 2.5 }}
            onClick={() => query.refetch()}
            disabled={query.isFetching}
          >
            Refresh Status
          </Button>
        </Stack>
      </Box>

      {query.isLoading && (
        <Box role="status" sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress aria-label="Checking system health" />
        </Box>
      )}

      {query.isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          System health could not be checked. {query.error.message}
          <Button color="inherit" size="small" startIcon={<RefreshRoundedIcon />} onClick={() => query.refetch()}>
            Retry
          </Button>
        </Alert>
      )}

      {health && (
        <>
          <Alert
            severity={health.database === 'healthy' ? 'success' : 'error'}
            sx={{ mb: 3 }}
          >
            Database connectivity is {health.database}. API status is {health.api}.
          </Alert>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <SurfaceCard variant="analytics" sx={{ p: 3, minWidth: 220 }}>
              <Typography variant="subtitle2">API</Typography>
              <Chip size="small" label={health.api} color={health.api === 'healthy' ? 'success' : 'error'} />
            </SurfaceCard>
            <SurfaceCard variant="task" sx={{ p: 3, minWidth: 220 }}>
              <Typography variant="subtitle2">Database</Typography>
              <Chip size="small" label={health.database} color={health.database === 'healthy' ? 'success' : 'error'} />
            </SurfaceCard>
            <SurfaceCard sx={{ p: 3, minWidth: 220 }}>
              <Typography variant="subtitle2">Last checked</Typography>
              <Typography variant="body1">
                {health.checkedAt ? new Date(health.checkedAt).toLocaleString() : 'Not reported'}
              </Typography>
            </SurfaceCard>
          </Stack>
        </>
      )}
    </AppLayout>
  );
}
