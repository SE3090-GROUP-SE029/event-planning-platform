import { useForm } from 'react-hook-form';
import { useNavigate, Link } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Paper,
  TextField,
  Typography,
} from '@mui/material';
import CalendarMonthRoundedIcon from '@mui/icons-material/CalendarMonthRounded';
import { useLogin } from '../api/authQueries';
import { useAuthStore } from '../../../shared/store/authStore';
import { tokens } from '../../../shared/theme/tokens';
import PublicNavbar from '../../../shared/components/layout/PublicNavbar';

export default function LoginPage() {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm();
  const navigate = useNavigate();
  const { mutate: login, isPending } = useLogin();
  const authError = useAuthStore((state) => state.error);

  const onSubmit = (data) => {
    login(data, {
      onSuccess: () => {
      navigate('/admin/dashboard');
      },
    });
  };

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: tokens.colors.canvas }}>
      <PublicNavbar />
      <Box
        component="main"
        sx={{
          minHeight: 'calc(100vh - 76px)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          p: 3,
        }}
      >
        <Container maxWidth="xs">
        <Paper
          sx={{
            p: { xs: 3, sm: 4.5 },
            width: '100%',
            borderRadius: tokens.radius.card,
            boxShadow: tokens.shadows.card,
            border: `1px solid ${tokens.colors.borderLight}`,
            backgroundColor: tokens.colors.surface,
          }}
        >
          <Box sx={{ textAlign: 'center', mb: 3.5 }}>
            <Box
              component={Link}
              to="/"
              sx={{
                width: 52,
                height: 52,
                borderRadius: '16px',
                backgroundColor: tokens.colors.pastelPink,
                color: tokens.colors.pastelPinkText,
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                mb: 1.5,
                textDecoration: 'none',
                boxShadow: tokens.shadows.soft,
              }}
            >
              <CalendarMonthRoundedIcon />
            </Box>
            <Typography component="h1" variant="h4" sx={{ fontWeight: 800, letterSpacing: '-0.04em' }}>
              Admin Login
            </Typography>
            <Typography variant="body2" sx={{ color: tokens.colors.textSecondary, mt: 0.5 }}>
              Administrator sign-in for the Plan It website
            </Typography>
          </Box>

          {authError && (
            <Alert severity="error" sx={{ mb: 3, borderRadius: '14px' }}>
              {authError}
            </Alert>
          )}

          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <TextField
              fullWidth
              label="Email Address"
              type="email"
              margin="normal"
              autoComplete="email"
              autoFocus
              {...register('email', {
                required: 'Email is required',
                pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Invalid email address' },
              })}
              error={!!errors.email}
              helperText={errors.email?.message}
            />

            <TextField
              fullWidth
              label="Password"
              type="password"
              margin="normal"
              autoComplete="current-password"
              {...register('password', {
                required: 'Password is required',
                minLength: { value: 6, message: 'Password must be at least 6 characters' },
              })}
              error={!!errors.password}
              helperText={errors.password?.message}
            />

            <Button
              type="submit"
              fullWidth
              variant="contained"
              size="large"
              sx={{ mt: 3, mb: 2, py: 1.4 }}
              disabled={isPending}
            >
              {isPending ? <CircularProgress size={24} color="inherit" /> : 'Sign In'}
            </Button>

            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', mt: 2, gap: 1 }}>
              <Typography variant="body2" sx={{ color: tokens.colors.textSecondary }}>
                Public information:
              </Typography>
              <Button
                component={Link}
                to="/"
                variant="text"
                size="small"
                sx={{ fontWeight: 700, color: tokens.colors.pastelBlueText, p: 0.5 }}
              >
                Return to Plan It
              </Button>
            </Box>
          </Box>
        </Paper>
        </Container>
      </Box>
    </Box>
  );
}
