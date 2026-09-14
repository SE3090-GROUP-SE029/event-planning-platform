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
  Grid,
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
    const { confirmPassword, ...payload } = data;
    registerUser(payload, {
      onSuccess: () => {
        navigate('/dashboard');
      },
    });
  };

  return (
    <Container maxWidth="sm">
      <Box sx={{ mt: 6, mb: 6, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
        <Paper
          elevation={4}
          sx={{
            p: 4,
            width: '100%',
            borderRadius: 3,
            border: '1px solid',
            borderColor: 'divider',
          }}
        >
          <Box sx={{ textAlign: 'center', mb: 3 }}>
            <Typography component="h1" variant="h4" fontWeight="bold" color="primary" gutterBottom>
              Join Plan It
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Create an account to start planning or offering services
            </Typography>
          </Box>

          {authError && (
            <Alert severity="error" sx={{ mb: 3 }}>
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
              <FormLabel component="legend">Account Type</FormLabel>
              <RadioGroup
                row
                value={selectedRole}
                onChange={(e) => setValue('role', e.target.value)}
              >
                <FormControlLabel
                  value="EVENT_PLANNER"
                  control={<Radio />}
                  label="Event Planner"
                />
                <FormControlLabel
                  value="VENDOR"
                  control={<Radio />}
                  label="Vendor / Supplier"
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
              sx={{ mt: 3, mb: 2, py: 1.3, fontWeight: 'bold' }}
              disabled={isPending}
            >
              {isPending ? <CircularProgress size={24} color="inherit" /> : 'Create Account'}
            </Button>

            <Divider sx={{ my: 2 }} />

            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Already have an account?
              </Typography>
              <Button
                component={Link}
                to="/login"
                variant="outlined"
                size="small"
                sx={{ textTransform: 'none' }}
              >
                Sign In
              </Button>
            </Box>
          </Box>
        </Paper>
      </Box>
    </Container>
  );
}
