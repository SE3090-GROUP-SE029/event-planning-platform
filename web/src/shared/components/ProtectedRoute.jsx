import { Navigate, useNavigate } from 'react-router-dom';
import { Typography, Button, Container, Paper, Box } from '@mui/material';
import LockPersonOutlinedIcon from '@mui/icons-material/LockPersonOutlined';
import { useAuthStore } from '../store/authStore';
import { hasAdminWebAccess } from '../auth/roleAccess';
import { tokens } from '../theme/tokens';

export default function ProtectedRoute({ children }) {
  const navigate = useNavigate();
  const token = useAuthStore((state) => state.accessToken);
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);

  if (!token || !user) {
    return <Navigate to="/login" replace />;
  }

  const userRoles = user.roles || [];

  if (!hasAdminWebAccess(userRoles)) {
    return (
      <Box sx={{ minHeight: '100vh', backgroundColor: 'background.default', display: 'flex', alignItems: 'center', justifyContent: 'center', p: 2 }}>
        <Container maxWidth="sm">
          <Paper sx={{ p: 4.5, textAlign: 'center', borderRadius: tokens.radius.card, boxShadow: tokens.shadows.card, backgroundColor: tokens.colors.surface, border: `1px solid ${tokens.colors.borderLight}` }}>
            <Box
              sx={{
                width: 56,
                height: 56,
                borderRadius: '50%',
                backgroundColor: tokens.colors.pastelPinkLight,
                color: tokens.colors.pastelPinkText,
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                mb: 2,
              }}
            >
              <LockPersonOutlinedIcon fontSize="medium" />
            </Box>
            <Typography variant="h4" fontWeight="800" sx={{ mb: 1.5, letterSpacing: '-0.03em' }}>
              Access Restricted
            </Typography>
            <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
              Only administrator accounts can access the web portal.
              <br />
              Your account is not authorized for website access.
            </Typography>
            <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', flexWrap: 'wrap' }}>
              <Button
                variant="contained"
                onClick={() => navigate('/')}
                sx={{
                  backgroundColor: tokens.colors.pastelBlue,
                  color: tokens.colors.pastelBlueText,
                  borderRadius: tokens.radius.pill,
                  px: 3,
                }}
              >
                Return to Plan It
              </Button>
              <Button
                variant="outlined"
                onClick={() => {
                  logout();
                  navigate('/login');
                }}
                sx={{ borderRadius: 9999, px: 3 }}
              >
                Sign Out
              </Button>
            </Box>
          </Paper>
        </Container>
      </Box>
    );
  }

  return children;
}
