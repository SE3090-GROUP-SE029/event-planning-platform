import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  TextField,
  Typography,
  Chip,
} from '@mui/material';
import { useAuthStore } from '../../../shared/store/authStore';
import {
  VENDOR_CATEGORIES,
  useCreateVendorProfile,
  useMyVendorProfile,
  useUpdateVendorProfile,
} from '../api/vendorApi';

export default function VendorProfilePage() {
  const isVendor = useAuthStore((state) => state.hasRole('VENDOR'));
  const { data: profile, isLoading, isError, error } = useMyVendorProfile();
  const createProfile = useCreateVendorProfile();
  const updateProfile = useUpdateVendorProfile();
  const isEditing = Boolean(profile);
  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm({
    defaultValues: {
      businessName: '',
      category: 'CATERING',
      contactEmail: '',
      contactPhone: '',
      description: '',
    },
  });

  const selectedCategory = watch('category');

  useEffect(() => {
    if (!profile) {
      return;
    }

    reset({
      businessName: profile.businessName ?? '',
      category: profile.category ?? 'CATERING',
      contactEmail: profile.contactEmail ?? '',
      contactPhone: profile.contactPhone ?? '',
      description: profile.description ?? '',
    });
  }, [profile, reset]);

  const onSubmit = (values) => {
    const payload = {
      businessName: values.businessName.trim(),
      category: values.category,
      contactEmail: values.contactEmail.trim(),
      contactPhone: values.contactPhone.trim(),
      description: values.description?.trim() || null,
    };

    if (isEditing) {
      updateProfile.mutate(payload);
    } else {
      createProfile.mutate(payload);
    }
  };

  if (!isVendor) {
    return (
      <Container maxWidth="sm" sx={{ mt: 8 }}>
        <Alert severity="warning">Vendor profile can only be managed with a Vendor account.</Alert>
        <Button component={Link} to="/dashboard" sx={{ mt: 2 }}>
          Back to dashboard
        </Button>
      </Container>
    );
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 10 }}>
        <CircularProgress />
      </Box>
    );
  }

  const mutation = isEditing ? updateProfile : createProfile;

  return (
    <Container maxWidth="sm">
      <Box sx={{ mt: 6, mb: 6 }}>
        <Paper elevation={4} sx={{ p: 4, borderRadius: 3 }}>
          <Typography variant="h4" fontWeight="bold" color="primary" gutterBottom>
            {isEditing ? 'Edit vendor profile' : 'Create vendor profile'}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Business details shown in the vendor marketplace.
          </Typography>

          {isEditing && (
            <Chip
              label={`Status: ${profile.status}`}
              color={profile.status === 'APPROVED' ? 'success' : 'warning'}
              sx={{ mb: 2 }}
            />
          )}

          {isError && !String(error?.message || '').toLowerCase().includes('not found') && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error.message}
            </Alert>
          )}

          {mutation.isError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {mutation.error.message}
            </Alert>
          )}

          {mutation.isSuccess && (
            <Alert severity="success" sx={{ mb: 2 }}>
              Vendor profile saved.
            </Alert>
          )}

          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <TextField
              fullWidth
              label="Business name"
              margin="dense"
              {...register('businessName', { required: 'Business name is required' })}
              error={!!errors.businessName}
              helperText={errors.businessName?.message}
            />

            <FormControl fullWidth margin="dense">
              <InputLabel id="vendor-category-label">Category</InputLabel>
              <Select
                labelId="vendor-category-label"
                label="Category"
                value={selectedCategory}
                onChange={(event) => setValue('category', event.target.value)}
              >
                {VENDOR_CATEGORIES.map((category) => (
                  <MenuItem key={category} value={category}>
                    {category}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <TextField
              fullWidth
              label="Contact email"
              type="email"
              margin="dense"
              {...register('contactEmail', {
                required: 'Contact email is required',
                pattern: {
                  value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                  message: 'Enter a valid email',
                },
              })}
              error={!!errors.contactEmail}
              helperText={errors.contactEmail?.message}
            />

            <TextField
              fullWidth
              label="Contact phone"
              margin="dense"
              {...register('contactPhone', { required: 'Contact phone is required' })}
              error={!!errors.contactPhone}
              helperText={errors.contactPhone?.message}
            />

            <TextField
              fullWidth
              label="Description"
              margin="dense"
              multiline
              minRows={3}
              {...register('description')}
            />

            <Button
              type="submit"
              fullWidth
              variant="contained"
              size="large"
              sx={{ mt: 3 }}
              disabled={mutation.isPending}
            >
              {mutation.isPending ? (
                <CircularProgress size={24} color="inherit" />
              ) : isEditing ? (
                'Save changes'
              ) : (
                'Create profile'
              )}
            </Button>

            <Button component={Link} to="/dashboard" fullWidth sx={{ mt: 1 }}>
              Back to dashboard
            </Button>
          </Box>
        </Paper>
      </Box>
    </Container>
  );
}
