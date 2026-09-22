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
  Select,
  TextField,
  Typography,
} from '@mui/material';
import { useAuthStore } from '../../../shared/store/authStore';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
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
    <Box sx={{ minHeight: '100vh', backgroundColor: '#F7F3E9', display: 'flex', alignItems: 'center', justifyContent: 'center', p: 2 }}>
      <Container maxWidth="sm">
        <SurfaceCard sx={{ p: 0, overflow: 'hidden' }}>
          <Box sx={{ p: { xs: 3, sm: 4 } }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 2, flexWrap: 'wrap', mb: 2 }}>
              <Box>
                <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
                  {isEditing ? 'Edit vendor profile' : 'Create vendor profile'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Business details shown in the vendor marketplace.
                </Typography>
              </Box>
              {isEditing && <StatusBadge status={profile.status} label={`Status: ${profile.status}`} />}
            </Box>

            {isError && !String(error?.message || '').toLowerCase().includes('not found') && (
              <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
                {error.message}
              </Alert>
            )}

            {mutation.isError && (
              <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
                {mutation.error.message}
              </Alert>
            )}

            {mutation.isSuccess && (
              <Alert severity="success" sx={{ mb: 2, borderRadius: 3 }}>
                Vendor profile saved.
              </Alert>
            )}

            <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
              <TextField fullWidth label="Business name" margin="dense" {...register('businessName', { required: 'Business name is required' })} error={!!errors.businessName} helperText={errors.businessName?.message} />

              <FormControl fullWidth margin="dense">
                <InputLabel id="vendor-category-label">Category</InputLabel>
                <Select labelId="vendor-category-label" label="Category" value={selectedCategory} onChange={(event) => setValue('category', event.target.value)}>
                  {VENDOR_CATEGORIES.map((category) => (
                    <MenuItem key={category} value={category}>
                      {category}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>

              <TextField fullWidth label="Contact email" type="email" margin="dense" {...register('contactEmail', { required: 'Contact email is required', pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Enter a valid email' } })} error={!!errors.contactEmail} helperText={errors.contactEmail?.message} />

              <TextField fullWidth label="Contact phone" margin="dense" {...register('contactPhone', { required: 'Contact phone is required' })} error={!!errors.contactPhone} helperText={errors.contactPhone?.message} />

              <TextField fullWidth label="Description" margin="dense" multiline minRows={3} {...register('description')} />

              <Button type="submit" fullWidth variant="contained" size="large" sx={{ mt: 3, borderRadius: 9999 }} disabled={mutation.isPending}>
                {mutation.isPending ? <CircularProgress size={24} color="inherit" /> : isEditing ? 'Save changes' : 'Create profile'}
              </Button>

              <Button component={Link} to="/dashboard" fullWidth variant="outlined" sx={{ mt: 1.5, borderRadius: 9999 }}>
                Back to dashboard
              </Button>
            </Box>
          </Box>
        </SurfaceCard>
      </Container>
    </Box>
  );
}
