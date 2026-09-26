import apiClient from '../../../shared/api/apiClient';

const formatIcsDate = (dateValue) => {
  const date = new Date(dateValue);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  return date.toISOString().replace(/[-:]/g, '').replace(/\.\d{3}Z$/, 'Z');
};

export const scheduleApi = {
  // GET /api/Schedules/event/{eventId}
  getScheduleByEventId: async (eventId) => {
    const response = await apiClient.get(`/api/Schedules/event/${eventId}`);
    return response.data;
  },

  // POST /api/Schedules/{scheduleId}/activities
  addActivity: async (scheduleId, payload) => {
    const response = await apiClient.post(`/api/Schedules/${scheduleId}/activities`, payload);
    return response.data;
  },

  // PATCH /api/Schedules/activities/{activityId}/status
  updateActivityStatus: async (activityId, status) => {
    const response = await apiClient.patch(`/api/Schedules/activities/${activityId}/status`, { status });
    return response.data;
  },

  downloadIcsFile: (eventName, activities = []) => {
    const safeEventName = (eventName || 'event').replace(/[^a-zA-Z0-9-_ ]/g, '').trim() || 'event';
    const lines = [
      'BEGIN:VCALENDAR',
      'VERSION:2.0',
      'PRODID:-//Event Planning Platform//EN',
      'CALSCALE:GREGORIAN',
      `X-WR-CALNAME:${safeEventName}`,
      'BEGIN:VTIMEZONE',
      'TZID:UTC',
      'BEGIN:STANDARD',
      'DTSTART:19700101T000000Z',
      'TZOFFSETFROM:+0000',
      'TZOFFSETTO:+0000',
      'END:STANDARD',
      'END:VTIMEZONE'
    ];

    activities.forEach((activity, index) => {
      const title = (activity.title || activity.name || `Activity ${index + 1}`).replace(/\\n/g, ' ');
      const description = (activity.description || '').replace(/\\n/g, ' ');
      const start = formatIcsDate(activity.startTime || activity.start || activity.date);
      const end = formatIcsDate(activity.endTime || activity.end || activity.startTime || activity.start || activity.date);

      if (!start || !end) {
        return;
      }

      lines.push('BEGIN:VEVENT');
      lines.push(`UID:${activity.id || `${Date.now()}-${index}`}`);
      lines.push(`DTSTAMP:${formatIcsDate(new Date())}`);
      lines.push(`DTSTART:${start}`);
      lines.push(`DTEND:${end}`);
      lines.push(`SUMMARY:${title}`);
      if (description) {
        lines.push(`DESCRIPTION:${description}`);
      }
      lines.push('END:VEVENT');
    });

    lines.push('END:VCALENDAR');

    const icsContent = `${lines.join('\r\n')}\r\n`;
    const blob = new Blob([icsContent], { type: 'text/calendar;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${safeEventName}.ics`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }
};