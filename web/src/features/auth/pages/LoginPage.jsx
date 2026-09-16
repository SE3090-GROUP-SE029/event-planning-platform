import { useForm } from 'react-hook-form';
import { useNavigate, Link } from 'react-router-dom';
import {
  Box,
  Button,
  Container,
  TextField,
  Typography,
  Alert,
  CircularProgress,
  Paper,
  Divider,
} from '@mui/material';
import { useLogin } from '../api/authQueries';
import { useAuthStore } from '../../../shared/store/authStore';

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
        navigate('/dashboard');
      },
    });
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        backgroundColor: '#F7F3E9',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        p: 2,
      }}
    >
      <Container maxWidth="xs">
        <Paper
          sx={{
            p: { xs: 3, sm: 4.5 },
            width: '100%',
            borderRadius: '24px',
            boxShadow: '0 8px 32px rgba(35, 25, 15, 0.05)',
            border: 'none',
            backgroundColor: '#FFFFFF',
          }}
        >
          {/* Brand Logo & Header */}
          <Box sx={{ textAlign: 'center', mb: 3.5 }}>
            <Box
              sx={{
                width: 48,
                height: 48,
                borderRadius: '50%',
                backgroundColor: '#F9BFD8',
                color: '#19191C',
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                mb: 1.5,
              }}
            >
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <rect x="3" y="4" width="18" height="18" rx="3" />
                <line x1="16" y1="2" x2="16" y2="6" />
                <line x1="8" y1="2" x2="8" y2="6" />
                <line x1="3" y1="10" x2="21" y2="10" />
              </svg>
            </Box>
            <Typography
              component="h1"
              variant="h5"
              sx={{ fontWeight: 700, letterSpacing: '-0.02em', color: '#1E1E22' }}
            >
              Plan It
            </Typography>
            <Typography variant="body2" sx={{ color: '#636369', mt: 0.5 }}>
              Sign in to access your pastel workspace
            </Typography>
          </Box>

          {authError && (
            <Alert severity="error" sx={{ mb: 3, borderRadius: '12px' }}>
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
                pattern: {
                  value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                  message: 'Invalid email address',
                },
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
              sx={{
                mt: 3,
                mb: 2,
                py: 1.4,
                backgroundColor: '#19191C',
                color: '#FFFFFF',
                borderRadius: 9999,
                fontWeight: 600,
                fontSize: '14px',
                '&:hover': { backgroundColor: '#2E2E36' },
              }}
              disabled={isPending}
            >
              {isPending ? <CircularProgress size={24} color="inherit" /> : 'Sign In'}
            </Button>

            <Divider sx={{ my: 2.5, borderColor: 'rgba(0,0,0,0.06)' }} />

            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 1 }}>
              <Typography variant="body2" sx={{ color: '#636369' }}>
                Don't have an account?
              </Typography>
              <Button
                component={Link}
                to="/register"
                variant="outlined"
                size="small"
                sx={{
                  textTransform: 'none',
                  borderRadius: 9999,
                  borderColor: 'rgba(0,0,0,0.1)',
                  color: '#1E1E22',
                  fontSize: '12px',
                  fontWeight: 600,
                }}
              >
                Register here
              </Button>
            </Box>

            <Box sx={{ textAlign: 'center', mt: 2.5 }}>
              <Button
                component={Link}
                to="/test"
                size="small"
                sx={{
                  color: '#8F8F96',
                  fontSize: '11px',
                  textTransform: 'none',
                  '&:hover': { color: '#1E1E22' },
                }}
              >
                Go to Backend Integration Test &rarr;
              </Button>
            </Box>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
}