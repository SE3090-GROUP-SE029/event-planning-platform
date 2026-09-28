import { Box, Typography } from '@mui/material';
import AppLayout from '../../../shared/components/layout/AppLayout';
import AdminDashboardOverview from '../components/AdminDashboardOverview';

export default function DashboardPage() {
  return (
    <AppLayout
      activeTab="dashboard"
      title="Admin Dashboard"
      subtitle="Platform overview"
    >
      <Box sx={{ mb: 3 }}>
        <Typography variant="h3" sx={{ mb: 0.5 }}>Admin Dashboard</Typography>
        <Typography variant="body1" color="text.secondary">
          Platform activity, event operations, and AI planning overview.
        </Typography>
      </Box>

      <AdminDashboardOverview />
    </AppLayout>
  );
}