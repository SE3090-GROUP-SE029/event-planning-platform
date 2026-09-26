import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { scheduleApi } from '../api/scheduleApi';
import { ConflictAlertBanner } from '../components/ConflictAlertBanner';
import { AddActivityModal } from '../components/AddActivityModal';

const ACTIVITY_STATUS_OPTIONS = [
  { value: 0, label: 'Scheduled', badgeColor: '#E0F2FE', textColor: '#0369A1' },
  { value: 1, label: 'In Progress', badgeColor: '#FEF3C7', textColor: '#B45309' },
  { value: 2, label: 'Completed', badgeColor: '#DCFCE7', textColor: '#15803D' },
  { value: 3, label: 'Skipped', badgeColor: '#FEE2E2', textColor: '#B91C1C' }
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
    skipped: 3
  };

  return mapping[normalized] ?? 0;
};

export const ScheduleBuilderPage = () => {
  const { eventId } = useParams();
  const [schedule, setSchedule] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [filterQuery, setFilterQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');

  const loadSchedule = async () => {
    try {
      setLoading(true);
      const data = await scheduleApi.getScheduleByEventId(eventId);
      setSchedule(data);
      setError(null);
    } catch (err) {
      setError(err?.response?.data?.message || 'Failed to load event schedule.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!eventId) return;

    let cancelled = false;

    const fetchSchedule = async () => {
      try {
        setLoading(true);
        const data = await scheduleApi.getScheduleByEventId(eventId);
        if (!cancelled) {
          setSchedule(data);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err?.response?.data?.message || 'Failed to load event schedule.');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    fetchSchedule();

    return () => {
      cancelled = true;
    };
  }, [eventId]);

  const handleAddActivity = async (payload) => {
    if (!schedule?.id) return;
    await scheduleApi.addActivity(schedule.id, payload);
    await loadSchedule();
  };

  const handleStatusChange = async (activityId, nextStatus) => {
    if (!schedule) return;

    const previousSchedule = schedule;
    setSchedule({
      ...previousSchedule,
      activities: (previousSchedule.activities || []).map((activity) =>
        activity.id === activityId ? { ...activity, status: nextStatus } : activity
      )
    });

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
        const matchesQuery = !filterQuery ||
          (activity.title && activity.title.toLowerCase().includes(filterQuery.toLowerCase())) ||
          (activity.description && activity.description.toLowerCase().includes(filterQuery.toLowerCase()));
        const matchesStatus = statusFilter === 'all' || String(statusValue) === String(statusFilter);

        return matchesQuery && matchesStatus;
      })
      .sort((a, b) => new Date(a.startTime) - new Date(b.startTime));
  }, [schedule, filterQuery, statusFilter]);

  if (loading) return <div style={{ padding: '24px' }}>Loading schedule...</div>;
  if (error) return <div style={{ padding: '24px', color: '#EF4444' }}>Error: {error}</div>;

  return (
    <div style={{ padding: '24px', maxWidth: '1000px', margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px', gap: '16px' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '24px', color: '#0F172A' }}>Event Schedule Builder</h1>
          <p style={{ margin: '4px 0 0 0', color: '#64748B', fontSize: '14px' }}>
            Event ID: {eventId}
          </p>
        </div>
        <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
          <button
            type="button"
            onClick={() => scheduleApi.downloadIcsFile(schedule?.event?.name || 'Event', schedule?.activities || [])}
            style={{
              backgroundColor: '#0F766E',
              color: '#fff',
              border: 'none',
              borderRadius: '6px',
              padding: '10px 16px',
              fontWeight: 500,
              cursor: 'pointer'
            }}
          >
            Export Calendar (.ics)
          </button>
          <button
            type="button"
            onClick={() => setIsModalOpen(true)}
            style={{
              backgroundColor: '#0284C7',
              color: '#fff',
              border: 'none',
              borderRadius: '6px',
              padding: '10px 16px',
              fontWeight: 500,
              cursor: 'pointer'
            }}
          >
            + Add Activity
          </button>
        </div>
      </div>

      <ConflictAlertBanner conflicts={schedule?.conflicts || []} />

      <div style={{ marginBottom: '20px', display: 'flex', gap: '12px', flexWrap: 'wrap', alignItems: 'center' }}>
        <input
          type="text"
          placeholder="Search activities..."
          value={filterQuery}
          onChange={(event) => setFilterQuery(event.target.value)}
          style={{
            width: '100%',
            maxWidth: '320px',
            padding: '8px 12px',
            borderRadius: '6px',
            border: '1px solid #CBD5E1'
          }}
        />

        <select
          value={statusFilter}
          onChange={(event) => setStatusFilter(event.target.value)}
          style={{
            padding: '8px 12px',
            borderRadius: '6px',
            border: '1px solid #CBD5E1',
            backgroundColor: '#fff'
          }}
        >
          <option value="all">All statuses</option>
          {ACTIVITY_STATUS_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      <div style={{ background: '#fff', borderRadius: '12px', border: '1px solid #E2E8F0', padding: '20px' }}>
        {filteredActivities.length === 0 ? (
          <p style={{ textAlign: 'center', color: '#94A3B8', margin: '32px 0' }}>
            No activities match the current filters. Click "+ Add Activity" to begin building your timeline.
          </p>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            {filteredActivities.map((activity) => {
              const statusValue = normalizeStatus(activity.status);
              const selectedStatus = ACTIVITY_STATUS_OPTIONS.find((option) => option.value === statusValue) || ACTIVITY_STATUS_OPTIONS[0];

              return (
                <div
                  key={activity.id}
                  style={{
                    borderLeft: '4px solid #0284C7',
                    padding: '12px 16px',
                    backgroundColor: '#F8FAFC',
                    borderRadius: '0 8px 8px 0',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    gap: '16px',
                    flexWrap: 'wrap'
                  }}
                >
                  <div style={{ flex: 1, minWidth: '220px' }}>
                    <h4 style={{ margin: '0 0 4px 0', color: '#1E293B', fontSize: '16px' }}>{activity.title}</h4>
                    {activity.description && (
                      <p style={{ margin: '0 0 6px 0', color: '#64748B', fontSize: '13px' }}>{activity.description}</p>
                    )}
                    <span style={{ fontSize: '12px', color: '#0284C7', fontWeight: 500 }}>
                      {new Date(activity.startTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' })} - {' '}
                      {new Date(activity.endTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' })}
                    </span>
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '12px', flexWrap: 'wrap' }}>
                    <span
                      style={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        padding: '4px 8px',
                        borderRadius: '999px',
                        fontSize: '12px',
                        fontWeight: 600,
                        backgroundColor: selectedStatus.badgeColor,
                        color: selectedStatus.textColor
                      }}
                    >
                      {selectedStatus.label}
                    </span>

                    <select
                      value={statusValue}
                      onChange={(event) => handleStatusChange(activity.id, Number(event.target.value))}
                      style={{
                        minWidth: '160px',
                        padding: '8px 12px',
                        borderRadius: '6px',
                        border: '1px solid #CBD5E1',
                        backgroundColor: '#fff'
                      }}
                    >
                      {ACTIVITY_STATUS_OPTIONS.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>

                    {activity.assignedVendorId && (
                      <span style={{ fontSize: '12px', background: '#E0F2FE', color: '#0369A1', padding: '4px 8px', borderRadius: '4px' }}>
                        Vendor Assigned
                      </span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      <AddActivityModal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} onAddActivity={handleAddActivity} />
    </div>
  );
};