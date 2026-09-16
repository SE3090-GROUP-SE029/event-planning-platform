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
  FormControl,
  FormLabel,
  RadioGroup,
  FormControlLabel,
  Radio,
  Divider,
} from '@mui/material';
import { useRegister } from '../api/authQueries';
import { useAuthStore } from '../../../shared/store/authStore';

export default function RegisterPage() {
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm({
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      password: '',
      confirmPassword: '',
      role: 'EVENT_PLANNER',
    },
  });

  const navigate = useNavigate();
  const { mutate: registerUser, isPending } = useRegister();
  const authError = useAuthStore((state) => state.error);
  const password = watch('password');
  const selectedRole = watch('role');

  const onSubmit = (data) => {
    const payload = { ...data };
    delete payload.confirmPassword;
    registerUser(payload, {
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
      <Container maxWidth="sm">
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
              Join Plan It
            </Typography>
            <Typography variant="body2" sx={{ color: '#636369', mt: 0.5 }}>
              Create an account to start planning or offering services
            </Typography>
          </Box>

          {authError && (
            <Alert severity="error" sx={{ mb: 3, borderRadius: '12px' }}>
              {authError}
            </Alert>
          )}

          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <Box sx={{ display: 'flex', gap: 2, mb: 1 }}>
              <TextField
                fullWidth
                label="First Name"
                margin="dense"
                {...register('firstName', { required: 'First name is required' })}
                error={!!errors.firstName}
                helperText={errors.firstName?.message}
              />
              <TextField
                fullWidth
                label="Last Name"
                margin="dense"
                {...register('lastName', { required: 'Last name is required' })}
                error={!!errors.lastName}
                helperText={errors.lastName?.message}
              />
            </Box>

            <TextField
              fullWidth
              label="Email Address"
              type="email"
              margin="dense"
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

            <FormControl component="fieldset" sx={{ mt: 2, mb: 1, width: '100%' }}>
              <FormLabel component="legend" sx={{ fontSize: '13px', fontWeight: 600, color: '#1E1E22' }}>
                Account Type
              </FormLabel>
              <RadioGroup
                row
                value={selectedRole}
                onChange={(e) => setValue('role', e.target.value)}
              >
                <FormControlLabel
                  value="EVENT_PLANNER"
                  control={<Radio sx={{ color: '#19191C', '&.Mui-checked': { color: '#19191C' } }} />}
                  label={<Typography sx={{ fontSize: '13.5px' }}>Event Planner</Typography>}
                />
                <FormControlLabel
                  value="VENDOR"
                  control={<Radio sx={{ color: '#19191C', '&.Mui-checked': { color: '#19191C' } }} />}
                  label={<Typography sx={{ fontSize: '13.5px' }}>Vendor / Supplier</Typography>}
                />
              </RadioGroup>
            </FormControl>

            <TextField
              fullWidth
              label="Password"
              type="password"
              margin="dense"
              {...register('password', {
                required: 'Password is required',
                minLength: { value: 6, message: 'Minimum 6 characters' },
              })}
              error={!!errors.password}
              helperText={errors.password?.message}
            />

            <TextField
              fullWidth
              label="Confirm Password"
              type="password"
              margin="dense"
              {...register('confirmPassword', {
                validate: (value) => value === password || 'Passwords do not match',
              })}
              error={!!errors.confirmPassword}
              helperText={errors.confirmPassword?.message}
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
              {isPending ? <CircularProgress size={24} color="inherit" /> : 'Create Account'}
            </Button>

            <Divider sx={{ my: 2.5, borderColor: 'rgba(0,0,0,0.06)' }} />

            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 1 }}>
              <Typography variant="body2" sx={{ color: '#636369' }}>
                Already have an account?
              </Typography>
              <Button
                component={Link}
                to="/login"
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
                Sign In
              </Button>
            </Box>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
}
