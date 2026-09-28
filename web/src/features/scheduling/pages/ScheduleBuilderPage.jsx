import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth';
import FileDownloadOutlinedIcon from '@mui/icons-material/FileDownloadOutlined';
import AddCircleOutlineRoundedIcon from '@mui/icons-material/AddCircleOutlineRounded';
import { scheduleApi } from '../api/scheduleApi';
import { ConflictAlertBanner } from '../components/ConflictAlertBanner';
import { AddActivityModal } from '../components/AddActivityModal';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';

const ACTIVITY_STATUS_OPTIONS = [
  { value: 0, label: 'Scheduled', badgeColor: '#EEF5FC', textColor: '#153251' },
  { value: 1, label: 'In Progress', badgeColor: '#FEF8E4', textColor: '#3E340B' },
  { value: 2, label: 'Completed', badgeColor: '#F0F6EC', textColor: '#20361A' },
  { value: 3, label: 'Skipped', badgeColor: '#FDEEF5', textColor: '#481931' },
];

const normalizeStatus = (status) => {
  if (typeof status === 'number') {
    return status;
  }

  const normalized = String(status ?? '').trim().toLowerCase().replace(/[^a-z]/g, '');

  if (!normalized) return 0;

  const mapping = {
    scheduled: 0,
    inprogress: 1,
    inprogresss: 1,
    completed: 2,
    skipped: 3,
  };

  return mapping[normalized] ?? 0;
};

export const ScheduleBuilderPage = () => {
  const { eventId } = useParams();
  const navigate = useNavigate();
  const [inputEventId, setInputEventId] = useState(eventId || '');
  const [schedule, setSchedule] = useState(null);
  const [loading, setLoading] = useState(Boolean(eventId));
  const [error, setError] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [filterQuery, setFilterQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');

  const loadSchedule = async (idToLoad) => {
    const id = idToLoad || eventId;
    if (!id) return;
    try {
      setLoading(true);
      const data = await scheduleApi.getScheduleByEventId(id);
      setSchedule(data);
      setError(null);
    } catch (err) {
      setError(err?.response?.data?.message || 'Failed to load event schedule.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!eventId) return undefined;
    let cancelled = false;

    scheduleApi.getScheduleByEventId(eventId)
      .then((data) => {
        if (!cancelled) {
          setSchedule(data);
          setError(null);
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err?.response?.data?.message || 'Failed to load event schedule.');
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [eventId]);

  const handleOpenEventId = (e) => {
    e.preventDefault();
    if (inputEventId.trim()) {
      navigate(`/events/${inputEventId.trim()}/schedule`);
    }
  };

  const handleAddActivity = async (payload) => {
    if (!schedule?.id) return;
    await scheduleApi.addActivity(schedule.id, payload);
    await loadSchedule(eventId);
  };

  const handleStatusChange = async (activityId, nextStatus) => {
    if (!schedule) return;

    const previousSchedule = { ...schedule };
    const updatedActivities = schedule.activities.map((item) =>
      item.id === activityId ? { ...item, status: nextStatus } : item
    );

    setSchedule({ ...schedule, activities: updatedActivities });

    try {
      await scheduleApi.updateActivityStatus(activityId, nextStatus);
    } catch (err) {
      setSchedule(previousSchedule);
      setError(err?.response?.data?.message || 'Failed to update activity status.');
    }
  };

  const filteredActivities = useMemo(() => {
    return (schedule?.activities || [])
      .filter((activity) => {
        const statusValue = normalizeStatus(activity.status);
        const matchesQuery =
          !filterQuery ||
          (activity.title && activity.title.toLowerCase().includes(filterQuery.toLowerCase())) ||
          (activity.description && activity.description.toLowerCase().includes(filterQuery.toLowerCase()));
        const matchesStatus = statusFilter === 'all' || String(statusValue) === String(statusFilter);

        return matchesQuery && matchesStatus;
      })
      .sort((a, b) => new Date(a.startTime) - new Date(b.startTime));
  }, [schedule, filterQuery, statusFilter]);

  return (
    <AppLayout
      activeTab="schedule"
      title="Event Schedule Builder"
      subtitle={eventId ? `Schedule for Event #${eventId}` : 'Manage event timeline & activities'}
    >
      <Box sx={{ mb: 3 }}>
        <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
          Event Schedule
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Track timeline milestones, detect conflicting activity times, and export calendar feeds.
        </Typography>
      </Box>

      {/* Selector if no eventId in route */}
      {!eventId && (
        <SurfaceCard sx={{ p: 3, mb: 3 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1 }}>
            <CalendarMonthIcon sx={{ color: '#19191C' }} />
            <Typography variant="h5" sx={{ fontWeight: 800 }}>
              Load Event Schedule
            </Typography>
          </Box>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Enter an existing Event ID to inspect its activity timeline.
          </Typography>
          <Box component="form" onSubmit={handleOpenEventId} sx={{ display: 'flex', gap: 1.5, flexWrap: 'wrap' }}>
            <TextField
              size="small"
              placeholder="Enter Event GUID"
              value={inputEventId}
              onChange={(e) => setInputEventId(e.target.value)}
              sx={{ flex: 1, minWidth: 260 }}
            />
            <Button
              variant="contained"
              type="submit"
              sx={{ borderRadius: 9999, px: 3, bgcolor: '#19191C', color: '#FFFFFF' }}
            >
              Open Schedule
            </Button>
          </Box>
        </SurfaceCard>
      )}

      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 3 }}>
          {error}
        </Alert>
      )}

      {eventId && !loading && (
        <Stack spacing={2.5}>
          {/* Action Bar */}
          <SurfaceCard sx={{ p: 2.5 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
              <Box>
                <Typography variant="h6" sx={{ fontWeight: 800 }}>
                  {schedule?.event?.name || 'Scheduled Activities'}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Event ID: {eventId}
                </Typography>
              </Box>

              <Stack direction="row" spacing={1.5}>
                <Button
                  variant="outlined"
                  startIcon={<FileDownloadOutlinedIcon />}
                  onClick={() => scheduleApi.downloadIcsFile(schedule?.event?.name || 'Event', schedule?.activities || [])}
                  sx={{ borderRadius: 9999, px: 2.5 }}
                >
                  Export (.ics)
                </Button>
                <Button
                  variant="contained"
                  startIcon={<AddCircleOutlineRoundedIcon />}
                  onClick={() => setIsModalOpen(true)}
                  sx={{ borderRadius: 9999, px: 2.5, bgcolor: '#19191C', color: '#FFFFFF' }}
                >
                  Add Activity
                </Button>
              </Stack>
            </Box>
          </SurfaceCard>

          <ConflictAlertBanner conflicts={schedule?.conflicts || []} />

          {/* Filters */}
          <SurfaceCard sx={{ p: 2 }}>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: 'center' }}>
              <TextField
                size="small"
                fullWidth
                placeholder="Search activities by title or notes"
                value={filterQuery}
                onChange={(e) => setFilterQuery(e.target.value)}
              />
              <Select
                size="small"
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
                sx={{ minWidth: 160 }}
              >
                <MenuItem value="all">All Statuses</MenuItem>
                {ACTIVITY_STATUS_OPTIONS.map((opt) => (
                  <MenuItem key={opt.value} value={opt.value}>{opt.label}</MenuItem>
                ))}
              </Select>
            </Stack>
          </SurfaceCard>

          {/* Activities List */}
          <SurfaceCard sx={{ p: 3 }}>
            {filteredActivities.length === 0 ? (
              <Box sx={{ textAlign: 'center', py: 4 }}>
                <Typography color="text.secondary">
                  No activities found. Click &quot;Add Activity&quot; to begin building your timeline.
                </Typography>
              </Box>
            ) : (
              <Stack spacing={2}>
                {filteredActivities.map((act) => {
                  const statusVal = normalizeStatus(act.status);
                  const opt = ACTIVITY_STATUS_OPTIONS.find((o) => o.value === statusVal) || ACTIVITY_STATUS_OPTIONS[0];

                  return (
                    <Box
                      key={act.id}
                      sx={{
                        p: 2,
                        borderRadius: 3,
                        backgroundColor: '#FAF7EF',
                        borderLeft: `4px solid ${opt.textColor}`,
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        flexWrap: 'wrap',
                        gap: 2,
                      }}
                    >
                      <Box sx={{ flex: 1, minWidth: 200 }}>
                        <Typography sx={{ fontWeight: 700, fontSize: '1rem' }}>{act.title}</Typography>
                        {act.description && (
                          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                            {act.description}
                          </Typography>
                        )}
                        <Typography variant="caption" sx={{ fontWeight: 600, color: '#636369', mt: 0.5, display: 'block' }}>
                          {new Date(act.startTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' })} →{' '}
                          {new Date(act.endTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' })}
                        </Typography>
                      </Box>

                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                        <Chip
                          size="small"
                          label={opt.label}
                          sx={{ bgcolor: opt.badgeColor, color: opt.textColor, fontWeight: 700 }}
                        />
                        <Select
                          size="small"
                          value={statusVal}
                          onChange={(e) => handleStatusChange(act.id, Number(e.target.value))}
                          sx={{ height: 32, fontSize: '0.8rem', bgcolor: '#FFFFFF' }}
                        >
                          {ACTIVITY_STATUS_OPTIONS.map((o) => (
                            <MenuItem key={o.value} value={o.value}>{o.label}</MenuItem>
                          ))}
                        </Select>
                      </Box>
                    </Box>
                  );
                })}
              </Stack>
            )}
          </SurfaceCard>
        </Stack>
      )}

      {isModalOpen && (
        <AddActivityModal
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
          onSubmit={handleAddActivity}
        />
      )}
    </AppLayout>
  );
};

export default ScheduleBuilderPage;