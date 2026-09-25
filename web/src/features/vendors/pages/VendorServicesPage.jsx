import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
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
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import { useAuthStore } from '../../../shared/store/authStore';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import VendorWorkspaceLayout from '../components/VendorWorkspaceLayout';
import {
  useCreateVendorService,
  useDeleteVendorService,
  useMyVendorProfile,
  useMyVendorServices,
  useUpdateVendorService,
} from '../api/vendorApi';

const PRICING_TYPES = [
  { value: 'FIXED', label: 'Fixed (LKR)' },
  { value: 'PER_PERSON', label: 'Per person (LKR)' },
  { value: 'PER_HOUR', label: 'Per hour (LKR)' },
  { value: 'PER_DAY', label: 'Per day (LKR)' },
];

const emptyForm = { serviceName: '', description: '', price: '', pricingType: '' };

function formatServicePrice(price, pricingType) {
  if (price == null || price === '') {
    return 'Price not set';
  }

  const amount = Number(price);
  if (Number.isNaN(amount)) {
    return 'Price not set';
  }

  const formatted = `Rs. ${amount.toLocaleString('en-LK', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} LKR`;

  switch (pricingType) {
    case 'PER_PERSON':
      return `${formatted} · per person`;
    case 'PER_HOUR':
      return `${formatted} · per hour`;
    case 'PER_DAY':
      return `${formatted} · per day`;
    case 'FIXED':
      return `${formatted} · fixed`;
    default:
      return formatted;
  }
}

function toPricePayload(price, pricingType) {
  const trimmedPrice = typeof price === 'string' ? price.trim() : price;
  if (trimmedPrice === '' || trimmedPrice == null) {
    return { price: null, pricingType: null };
  }

  return {
    price: Number(trimmedPrice),
    pricingType: pricingType || null,
  };
}

export default function VendorServicesPage() {
  const navigate = useNavigate();
  const isVendor = useAuthStore((state) => state.hasRole('VENDOR'));
  const { data: profile, isLoading: profileLoading } = useMyVendorProfile();
  const hasProfile = Boolean(profile);
  const { data: services = [], isLoading, isError, error } = useMyVendorServices(hasProfile);
  const createService = useCreateVendorService();
  const updateService = useUpdateVendorService();
  const deleteService = useDeleteVendorService();
  const [editingId, setEditingId] = useState(null);
  const [confirmDeleteId, setConfirmDeleteId] = useState(null);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm({ defaultValues: emptyForm });

  const selectedPricingType = watch('pricingType');

  useEffect(() => {
    if (!editingId) {
      reset(emptyForm);
      return;
    }

    const current = services.find((item) => item.id === editingId);
    if (current) {
      reset({
        serviceName: current.serviceName ?? '',
        description: current.description ?? '',
        price: current.price == null ? '' : String(current.price),
        pricingType: current.pricingType ?? '',
      });
    }
  }, [editingId, services, reset]);

  const mutation = editingId ? updateService : createService;

  const onSubmit = (values) => {
    const pricing = toPricePayload(values.price, values.pricingType);
    const payload = {
      serviceName: values.serviceName.trim(),
      description: values.description?.trim() || null,
      ...pricing,
    };

    if (editingId) {
      updateService.mutate(
        { id: editingId, ...payload },
        {
          onSuccess: () => {
            setEditingId(null);
            reset(emptyForm);
          },
        },
      );
    } else {
      createService.mutate(payload, {
        onSuccess: () => reset(emptyForm),
      });
    }
  };

  if (!isVendor) {
    return (
      <VendorWorkspaceLayout activeTab="vendor-services" title="Vendor services">
        <Alert severity="warning">Vendor services can only be managed with a Vendor account.</Alert>
      </VendorWorkspaceLayout>
    );
  }

  if (profileLoading || (hasProfile && isLoading)) {
    return (
      <VendorWorkspaceLayout activeTab="vendor-services" title="Vendor services">
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
          <CircularProgress />
        </Box>
      </VendorWorkspaceLayout>
    );
  }

  if (!hasProfile) {
    return (
      <VendorWorkspaceLayout activeTab="vendor-services" title="Vendor services">
        <Alert severity="info" sx={{ mb: 2 }}>
          Create your vendor profile before adding services.
        </Alert>
        <Button variant="contained" sx={{ borderRadius: 9999 }} onClick={() => navigate('/vendor/profile')}>
          Create vendor profile
        </Button>
      </VendorWorkspaceLayout>
    );
  }

  return (
    <VendorWorkspaceLayout
      activeTab="vendor-services"
      title="Vendor services"
      subtitle="Add and manage the services you offer."
    >
      {(isError || mutation.isError || deleteService.isError) && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
          {error?.message || mutation.error?.message || deleteService.error?.message}
        </Alert>
      )}

      <SurfaceCard sx={{ p: { xs: 2.5, sm: 3 }, mb: 3, maxWidth: 860 }}>
        <Typography variant="h6" sx={{ fontWeight: 700, mb: 1.5 }}>
          {editingId ? 'Edit service' : 'Add service'}
        </Typography>
        <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
          <TextField
            fullWidth
            label="Service name"
            margin="dense"
            {...register('serviceName', { required: 'Service name is required' })}
            error={!!errors.serviceName}
            helperText={errors.serviceName?.message}
          />
          <TextField fullWidth label="Description" margin="dense" multiline minRows={3} {...register('description')} />
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ mt: 0.5 }}>
            <TextField
              fullWidth
              label="Price (Rs. / LKR)"
              margin="dense"
              type="number"
              inputProps={{ min: 0, step: '0.01' }}
              {...register('price', {
                validate: (value) => {
                  if (value === '' || value == null) return true;
                  const amount = Number(value);
                  if (Number.isNaN(amount) || amount < 0) {
                    return 'Enter a valid price of Rs. 0 or more';
                  }
                  if (!selectedPricingType) {
                    return 'Select a pricing type when setting a price';
                  }
                  return true;
                },
              })}
              error={!!errors.price}
              helperText={errors.price?.message || 'Leave blank to clear pricing'}
            />
            <FormControl fullWidth margin="dense">
              <InputLabel id="pricing-type-label">Pricing type</InputLabel>
              <Select
                labelId="pricing-type-label"
                label="Pricing type"
                value={selectedPricingType || ''}
                onChange={(event) => setValue('pricingType', event.target.value, { shouldValidate: true })}
              >
                <MenuItem value="">
                  <em>None</em>
                </MenuItem>
                {PRICING_TYPES.map((option) => (
                  <MenuItem key={option.value} value={option.value}>
                    {option.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ mt: 2 }}>
            <Button type="submit" variant="contained" sx={{ borderRadius: 9999 }} disabled={mutation.isPending}>
              {mutation.isPending ? <CircularProgress size={22} color="inherit" /> : editingId ? 'Save changes' : 'Add service'}
            </Button>
            {editingId && (
              <Button
                type="button"
                variant="outlined"
                sx={{ borderRadius: 9999 }}
                onClick={() => {
                  setEditingId(null);
                  reset(emptyForm);
                }}
              >
                Cancel edit
              </Button>
            )}
            {editingId && (
              <Button
                type="button"
                variant="text"
                sx={{ borderRadius: 9999 }}
                onClick={() => {
                  setValue('price', '');
                  setValue('pricingType', '');
                }}
              >
                Clear price
              </Button>
            )}
          </Stack>
        </Box>
      </SurfaceCard>

      <Stack spacing={1.5} sx={{ maxWidth: 860 }}>
        {services.length === 0 && (
          <SurfaceCard sx={{ p: 3 }}>
            <Typography color="text.secondary">No services yet. Add your first service above.</Typography>
          </SurfaceCard>
        )}

        {services.map((service) => (
          <SurfaceCard key={service.id} sx={{ p: 2.5 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, alignItems: 'flex-start' }}>
              <Box>
                <Typography variant="h6" sx={{ fontWeight: 700 }}>
                  {service.serviceName}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                  {service.description || 'No description'}
                </Typography>
                <Typography variant="body2" sx={{ mt: 1, fontWeight: 600 }}>
                  {formatServicePrice(service.price, service.pricingType)}
                </Typography>
              </Box>
              <Stack direction="row" spacing={0.5}>
                <IconButton aria-label="Edit service" onClick={() => setEditingId(service.id)}>
                  <EditOutlinedIcon />
                </IconButton>
                <IconButton aria-label="Delete service" color="error" onClick={() => setConfirmDeleteId(service.id)}>
                  <DeleteOutlinedIcon />
                </IconButton>
              </Stack>
            </Box>
          </SurfaceCard>
        ))}
      </Stack>

      <Dialog open={Boolean(confirmDeleteId)} onClose={() => setConfirmDeleteId(null)}>
        <DialogTitle>Delete service?</DialogTitle>
        <DialogContent>
          <Typography variant="body2">This removes the service from your vendor catalog. This cannot be undone.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmDeleteId(null)}>Cancel</Button>
          <Button
            color="error"
            disabled={deleteService.isPending}
            onClick={() => {
              deleteService.mutate(confirmDeleteId, {
                onSuccess: () => setConfirmDeleteId(null),
              });
            }}
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </VendorWorkspaceLayout>
  );
}
