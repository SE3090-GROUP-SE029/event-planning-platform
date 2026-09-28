import { Box, Typography } from '@mui/material';
import AppLayout from '../../../shared/components/layout/AppLayout';
import AdminDashboardOverview from '../components/AdminDashboardOverview';

export default function AdminAnalyticsPage() {
  return (
    <AppLayout
      activeTab="analytics"
      title="Platform Analytics"
      subtitle="Real-time performance and system intelligence"
    >
      <Box sx={{ mb: 3 }}>
        <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
          Analytics & Metrics
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Comprehensive breakdown of platform adoption, event pipelines, and AI coordination.
        </Typography>
      </Box>

      <AdminDashboardOverview />
    </AppLayout>
  );
}
