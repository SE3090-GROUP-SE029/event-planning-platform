import { useEffect, useRef } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import {
  Alert,
  Avatar,
  Box,
  Button,
  CircularProgress,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined';
import StorefrontOutlinedIcon from '@mui/icons-material/StorefrontOutlined';
import { useAuthStore } from '../../../shared/store/authStore';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import VendorWorkspaceLayout from '../components/VendorWorkspaceLayout';
import {
  VENDOR_CATEGORIES,
  resolveVendorImageUrl,
  useCreateVendorProfile,
  useDeleteVendorGalleryImage,
  useMyVendorGallery,
  useMyVendorProfile,
  useUpdateVendorProfile,
  useUploadVendorGalleryImage,
  useUploadVendorProfileImage,
} from '../api/vendorApi';

export default function VendorProfilePage() {
  const isVendor = useAuthStore((state) => state.hasRole('VENDOR'));
  const logoInputRef = useRef(null);
  const galleryInputRef = useRef(null);
  const { data: profile, isLoading, isError, error } = useMyVendorProfile();
  const isEditing = Boolean(profile);
  const { data: gallery = [], isLoading: galleryLoading } = useMyVendorGallery(isEditing);
  const createProfile = useCreateVendorProfile();
  const updateProfile = useUpdateVendorProfile();
  const uploadImage = useUploadVendorProfileImage();
  const uploadGalleryImage = useUploadVendorGalleryImage();
  const deleteGalleryImage = useDeleteVendorGalleryImage();
  const {
    register,
    handleSubmit,
    reset,
    control,
    setValue,
    formState: { errors },
  } = useForm({
    defaultValues: {
      businessName: '',
      category: 'CATERING',
      contactEmail: '',
      contactPhone: '',
      address: '',
      description: '',
      websiteUrl: '',
    },
  });

  const selectedCategory = useWatch({
    control,
    name: 'category',
  });
  const imageUrl = resolveVendorImageUrl(profile?.profileImageUrl);

  useEffect(() => {
    if (!profile) {
      return;
    }

    reset({
      businessName: profile.businessName ?? '',
      category: profile.category ?? 'CATERING',
      contactEmail: profile.contactEmail ?? '',
      contactPhone: profile.contactPhone ?? '',
      address: profile.address ?? '',
      description: profile.description ?? '',
      websiteUrl: profile.websiteUrl ?? '',
    });
  }, [profile, reset]);

  const onSubmit = (values) => {
    const payload = {
      businessName: values.businessName.trim(),
      category: values.category,
      contactEmail: values.contactEmail.trim(),
      contactPhone: values.contactPhone.trim(),
      address: values.address.trim(),
      description: values.description?.trim() || null,
      websiteUrl: values.websiteUrl?.trim() || null,
    };

    if (isEditing) {
      updateProfile.mutate(payload);
    } else {
      createProfile.mutate(payload);
    }
  };

  const onLogoSelected = (event) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;
    uploadImage.mutate(file);
  };

  const onGallerySelected = (event) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;
    uploadGalleryImage.mutate(file);
  };

  if (!isVendor) {
    return (
      <VendorWorkspaceLayout activeTab="vendor-profile" title="Vendor profile">
        <Alert severity="warning">Vendor profile can only be managed with a Vendor account.</Alert>
      </VendorWorkspaceLayout>
    );
  }

  if (isLoading) {
    return (
      <VendorWorkspaceLayout activeTab="vendor-profile" title="Vendor profile">
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
          <CircularProgress />
        </Box>
      </VendorWorkspaceLayout>
    );
  }

  const mutation = isEditing ? updateProfile : createProfile;

  return (
    <VendorWorkspaceLayout
      activeTab="vendor-profile"
      title={isEditing ? 'Edit vendor profile' : 'Create vendor profile'}
      subtitle="Business details shown in the vendor marketplace."
    >
      <Stack spacing={2.5} sx={{ maxWidth: 860 }}>
        <SurfaceCard sx={{ p: { xs: 2.5, sm: 3.5 } }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 2, flexWrap: 'wrap', mb: 2 }}>
            <Typography variant="h4" sx={{ fontWeight: 800, letterSpacing: '-0.04em' }}>
              {isEditing ? 'Edit vendor profile' : 'Create vendor profile'}
            </Typography>
            {isEditing && <StatusBadge status={profile.status} label={`Status: ${profile.status}`} />}
          </Box>

          <Typography variant="h6" sx={{ fontWeight: 700, mb: 1.5 }}>
            Business logo
          </Typography>
          <Stack alignItems="center" spacing={1.5} sx={{ mb: 3 }}>
            <Avatar
              src={imageUrl || undefined}
              sx={{ width: 112, height: 112, bgcolor: '#E8E4DA', color: '#19191C', fontSize: 42 }}
            >
              {!imageUrl && <StorefrontOutlinedIcon fontSize="inherit" />}
            </Avatar>
            <input
              ref={logoInputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              hidden
              onChange={onLogoSelected}
            />
            <Button
              variant="outlined"
              sx={{ borderRadius: 9999 }}
              disabled={!isEditing || uploadImage.isPending}
              onClick={() => logoInputRef.current?.click()}
            >
              {uploadImage.isPending ? 'Uploading…' : isEditing ? 'Upload / change logo' : 'Save profile first to upload logo'}
            </Button>
            {uploadImage.isError && (
              <Alert severity="error" sx={{ width: '100%', borderRadius: 3 }}>
                {uploadImage.error.message}
              </Alert>
            )}
          </Stack>

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

            <TextField fullWidth label="Address" margin="dense" {...register('address', { required: 'Address is required' })} error={!!errors.address} helperText={errors.address?.message} />

            <TextField fullWidth label="Website / social link" margin="dense" placeholder="https://…" {...register('websiteUrl')} />

            <TextField fullWidth label="Description" margin="dense" multiline minRows={3} {...register('description')} />

            <Button type="submit" fullWidth variant="contained" size="large" sx={{ mt: 3, borderRadius: 9999 }} disabled={mutation.isPending}>
              {mutation.isPending ? <CircularProgress size={24} color="inherit" /> : isEditing ? 'Save changes' : 'Create profile'}
            </Button>
          </Box>
        </SurfaceCard>

        <SurfaceCard sx={{ p: { xs: 2.5, sm: 3.5 } }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, alignItems: 'center', flexWrap: 'wrap', mb: 1.5 }}>
            <Box>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>
                Business images
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Add photos of your work. These appear on your vendor dashboard.
              </Typography>
            </Box>
            <input
              ref={galleryInputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              hidden
              onChange={onGallerySelected}
            />
            <Button
              variant="contained"
              sx={{ borderRadius: 9999 }}
              disabled={!isEditing || uploadGalleryImage.isPending || gallery.length >= 8}
              onClick={() => galleryInputRef.current?.click()}
            >
              {uploadGalleryImage.isPending ? 'Uploading…' : 'Add image'}
            </Button>
          </Box>

          {!isEditing && (
            <Alert severity="info" sx={{ borderRadius: 3 }}>
              Save your vendor profile first, then you can add business images.
            </Alert>
          )}

          {(uploadGalleryImage.isError || deleteGalleryImage.isError) && (
            <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
              {uploadGalleryImage.error?.message || deleteGalleryImage.error?.message}
            </Alert>
          )}

          {isEditing && galleryLoading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
              <CircularProgress size={28} />
            </Box>
          )}

          {isEditing && !galleryLoading && gallery.length === 0 && (
            <Typography color="text.secondary">No business images yet. Click Add image to upload.</Typography>
          )}

          {isEditing && gallery.length > 0 && (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr 1fr', sm: 'repeat(3, 1fr)' },
                gap: 1.5,
                mt: 1,
              }}
            >
              {gallery.map((image) => {
                const src = resolveVendorImageUrl(image.imageUrl);
                return (
                  <Box
                    key={image.id}
                    sx={{
                      position: 'relative',
                      borderRadius: 3,
                      overflow: 'hidden',
                      backgroundColor: '#EFEAE0',
                      aspectRatio: '4 / 3',
                    }}
                  >
                    <Box
                      component="img"
                      src={src || undefined}
                      alt="Vendor gallery"
                      sx={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
                    />
                    <IconButton
                      aria-label="Delete image"
                      size="small"
                      onClick={() => deleteGalleryImage.mutate(image.id)}
                      sx={{
                        position: 'absolute',
                        top: 8,
                        right: 8,
                        backgroundColor: 'rgba(255,255,255,0.9)',
                        '&:hover': { backgroundColor: '#fff' },
                      }}
                    >
                      <DeleteOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Box>
                );
              })}
            </Box>
          )}
        </SurfaceCard>
      </Stack>
    </VendorWorkspaceLayout>
  );
}
