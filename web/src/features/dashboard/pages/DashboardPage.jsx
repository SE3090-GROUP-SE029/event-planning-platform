import { Box, Button, Card, CardContent, Chip, Container, Grid, Typography, Paper } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../../shared/store/authStore';

export default function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      {/* Header */}
      <Paper elevation={2} sx={{ p: 3, mb: 4, borderRadius: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Box>
          <Typography variant="h4" fontWeight="bold" color="primary">
            Plan It Dashboard
          </Typography>
          <Typography variant="subtitle1" color="text.secondary">
            Welcome back, {user?.email || 'User'}!
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button variant="outlined" onClick={() => navigate('/test')}>
            API Integration Test
          </Button>
          <Button variant="contained" color="error" onClick={handleLogout}>
            Sign Out
          </Button>
        </Box>
      </Paper>

      <Grid container spacing={3}>
        {/* User Profile Card */}
        <Grid item xs={12} md={4}>
          <Card sx={{ borderRadius: 3, height: '100%' }}>
            <CardContent>
              <Typography variant="h6" fontWeight="bold" gutterBottom>
                Account Profile
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                User ID:
              </Typography>
              <Typography variant="body2" sx={{ fontFamily: 'monospace', wordBreak: 'break-all' }}>
                {user?.id || 'N/A'}
              </Typography>

              <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
                Email:
              </Typography>
              <Typography variant="body1" fontWeight="medium">
                {user?.email}
              </Typography>

              <Typography variant="body2" color="text.secondary" sx={{ mt: 2, mb: 1 }}>
                Assigned Roles:
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                {user?.roles?.map((role) => (
                  <Chip
                    key={role}
                    label={role}
                    color={role === 'ADMIN' ? 'error' : role === 'EVENT_PLANNER' ? 'primary' : 'success'}
                    size="small"
                  />
                ))}
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Quick Actions Card */}
        <Grid item xs={12} md={8}>
          <Card sx={{ borderRadius: 3, height: '100%' }}>
            <CardContent>
              <Typography variant="h6" fontWeight="bold" gutterBottom>
                Platform Capabilities
              </Typography>
              <Typography variant="body2" color="text.secondary" paragraph>
                Clean Architecture integration is fully operational across .NET 8 backend, React Web, and Flutter mobile apps.
              </Typography>
              <Box sx={{ display: 'flex', gap: 2, mt: 3, flexWrap: 'wrap' }}>
                <Button variant="contained" color="primary" onClick={() => navigate('/test')}>
                  Run Integration Test Suite
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Container>
  );
}
