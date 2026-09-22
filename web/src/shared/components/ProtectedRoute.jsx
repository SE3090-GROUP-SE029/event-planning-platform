import { Navigate } from 'react-router-dom';
import { Typography, Button, Container, Paper } from '@mui/material';
import { useAuthStore } from '../store/authStore';

export default function ProtectedRoute({ children, requiredRole }) {
  const token = useAuthStore((state) => state.accessToken);
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);

  if (!token || !user) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole && !user.roles?.includes(requiredRole.toUpperCase())) {
    return (
      <Container maxWidth="sm" sx={{ mt: 10 }}>
        <Paper elevation={3} sx={{ p: 4, textAlign: 'center', borderRadius: 3 }}>
          <Typography variant="h4" color="error" fontWeight="bold" sx={{ mb: 2 }}>
            Access Denied
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
            This resource requires the <strong>{requiredRole}</strong> role.
            <br />
            Your active roles: <strong>{user.roles?.join(', ') || 'None'}</strong>
          </Typography>
          <Button variant="contained" onClick={logout}>
            Sign Out
          </Button>
        </Paper>
      </Container>
    );
  }

  return children;
}
