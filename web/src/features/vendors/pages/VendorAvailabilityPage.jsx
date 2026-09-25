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
  FormControlLabel,
  IconButton,
  Radio,
  RadioGroup,
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
  useCreateVendorAvailability,
  useDeleteVendorAvailability,
  useMyVendorAvailability,
  useMyVendorProfile,
  useUpdateVendorAvailability,
} from '../api/vendorApi';

const emptyForm = {
  startDateTime: '',
  endDateTime: '',
  isAvailable: 'true',
};

function toLocalInputValue(iso) {
  if (!iso) return '';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';
  const pad = (n) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function toUtcIso(localValue) {
  const date = new Date(localValue);
  if (Number.isNaN(date.getTime())) {
    throw new Error('Enter valid start and end date/times.');
  }
  return date.toISOString();
}

function formatPeriod(iso) {
  return new Date(iso).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

export default function VendorAvailabilityPage() {
  const navigate = useNavigate();
  const isVendor = useAuthStore((state) => state.hasRole('VENDOR'));
  const { data: profile, isLoading: profileLoading } = useMyVendorProfile();
  const hasProfile = Boolean(profile);
  const { data: periods = [], isLoading, isError, error } = useMyVendorAvailability(hasProfile);
  const createAvailability = useCreateVendorAvailability();
  const updateAvailability = useUpdateVendorAvailability();
  const deleteAvailability = useDeleteVendorAvailability();
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

  const isAvailableValue = watch('isAvailable');

  useEffect(() => {
    if (!editingId) {
      reset(emptyForm);
      return;
    }

    const current = periods.find((item) => item.id === editingId);
    if (current) {
      reset({
        startDateTime: toLocalInputValue(current.startDateTime),
        endDateTime: toLocalInputValue(current.endDateTime),
        isAvailable: current.isAvailable ? 'true' : 'false',
      });
    }
  }, [editingId, periods, reset]);

  const mutation = editingId ? updateAvailability : createAvailability;

  const onSubmit = (values) => {
    let payload;
    try {
      payload = {
        startDateTime: toUtcIso(values.startDateTime),
        endDateTime: toUtcIso(values.endDateTime),
        isAvailable: values.isAvailable === 'true',
      };
    } catch (err) {
      return;
    }

    if (editingId) {
      updateAvailability.mutate(
        { id: editingId, ...payload },
        {
          onSuccess: () => {
            setEditingId(null);
            reset(emptyForm);
          },
        },
      );
    } else {
      createAvailability.mutate(payload, {
        onSuccess: () => reset(emptyForm),
      });
    }
  };

  if (!isVendor) {
    return (
      <VendorWorkspaceLayout activeTab="vendor-availability" title="Vendor availability">
        <Alert severity="warning">Availability can only be managed with a Vendor account.</Alert>
      </VendorWorkspaceLayout>
    );
  }

  if (profileLoading || (hasProfile && isLoading)) {
    return (
      <VendorWorkspaceLayout activeTab="vendor-availability" title="Vendor availability">
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
          <CircularProgress />
        </Box>
      </VendorWorkspaceLayout>
    );
  }

  if (!hasProfile) {
    return (
      <VendorWorkspaceLayout activeTab="vendor-availability" title="Vendor availability">
        <Alert severity="info" sx={{ mb: 2 }}>
          Create your vendor profile before managing availability.
        </Alert>
        <Button variant="contained" sx={{ borderRadius: 9999 }} onClick={() => navigate('/vendor/profile')}>
          Create vendor profile
        </Button>
      </VendorWorkspaceLayout>
    );
  }

  return (
    <VendorWorkspaceLayout
      activeTab="vendor-availability"
      title="Vendor availability"
      subtitle="Set when your business is available or unavailable for events."
    >
      {(isError || mutation.isError || deleteAvailability.isError) && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
          {error?.message || mutation.error?.message || deleteAvailability.error?.message}
        </Alert>
      )}

      <SurfaceCard sx={{ p: { xs: 2.5, sm: 3 }, mb: 3, maxWidth: 860 }}>
        <Typography variant="h6" sx={{ fontWeight: 700, mb: 1.5 }}>
          {editingId ? 'Edit availability' : 'Add availability'}
        </Typography>
        <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
            <TextField
              fullWidth
              label="Start date/time"
              type="datetime-local"
              margin="dense"
              InputLabelProps={{ shrink: true }}
              {...register('startDateTime', { required: 'Start date/time is required' })}
              error={!!errors.startDateTime}
              helperText={errors.startDateTime?.message}
            />
            <TextField
              fullWidth
              label="End date/time"
              type="datetime-local"
              margin="dense"
              InputLabelProps={{ shrink: true }}
              {...register('endDateTime', {
                required: 'End date/time is required',
                validate: (value, formValues) => {
                  if (!value || !formValues.startDateTime) return true;
                  if (new Date(formValues.startDateTime) >= new Date(value)) {
                    return 'Start must be before end';
                  }
                  return true;
                },
              })}
              error={!!errors.endDateTime}
              helperText={errors.endDateTime?.message}
            />
          </Stack>
          <FormControl component="fieldset" sx={{ mt: 1.5 }}>
            <Typography variant="body2" sx={{ mb: 0.5, fontWeight: 600 }}>
              Status
            </Typography>
            <RadioGroup
              row
              value={isAvailableValue}
              onChange={(event) => setValue('isAvailable', event.target.value)}
            >
              <FormControlLabel value="true" control={<Radio />} label="Available" />
              <FormControlLabel value="false" control={<Radio />} label="Unavailable" />
            </RadioGroup>
          </FormControl>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ mt: 2 }}>
            <Button type="submit" variant="contained" sx={{ borderRadius: 9999 }} disabled={mutation.isPending}>
              {mutation.isPending ? <CircularProgress size={22} color="inherit" /> : editingId ? 'Save changes' : 'Add period'}
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
          </Stack>
        </Box>
      </SurfaceCard>

      <Stack spacing={1.5} sx={{ maxWidth: 860 }}>
        {periods.length === 0 && (
          <SurfaceCard sx={{ p: 3 }}>
            <Typography color="text.secondary">
              No availability periods yet. Dates without an available period stay not bookable.
            </Typography>
          </SurfaceCard>
        )}

        {periods.map((period) => (
          <SurfaceCard key={period.id} sx={{ p: 2.5 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, alignItems: 'flex-start' }}>
              <Box>
                <Typography variant="h6" sx={{ fontWeight: 700 }}>
                  {period.isAvailable ? 'Available' : 'Unavailable'}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                  {formatPeriod(period.startDateTime)} → {formatPeriod(period.endDateTime)}
                </Typography>
              </Box>
              <Stack direction="row" spacing={0.5}>
                <IconButton aria-label="Edit availability" onClick={() => setEditingId(period.id)}>
                  <EditOutlinedIcon />
                </IconButton>
                <IconButton aria-label="Delete availability" color="error" onClick={() => setConfirmDeleteId(period.id)}>
                  <DeleteOutlinedIcon />
                </IconButton>
              </Stack>
            </Box>
          </SurfaceCard>
        ))}
      </Stack>

      <Dialog open={Boolean(confirmDeleteId)} onClose={() => setConfirmDeleteId(null)}>
        <DialogTitle>Delete availability?</DialogTitle>
        <DialogContent>
          <Typography variant="body2">This removes the period from your availability calendar.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmDeleteId(null)}>Cancel</Button>
          <Button
            color="error"
            disabled={deleteAvailability.isPending}
            onClick={() => {
              deleteAvailability.mutate(confirmDeleteId, {
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
