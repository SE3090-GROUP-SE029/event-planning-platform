

export const ConflictAlertBanner = ({ conflicts = [] }) => {
  if (!conflicts || conflicts.length === 0) return null;

  return (
    <div style={{
      backgroundColor: '#FEF2F2',
      border: '1px solid #F87171',
      borderRadius: '8px',
      padding: '16px',
      marginBottom: '20px'
    }}>
      <div style={{ display: 'flex', alignItems: 'center', marginBottom: '8px' }}>
        <span style={{ fontSize: '18px', marginRight: '8px' }}>⚠️</span>
        <h4 style={{ margin: 0, color: '#991B1B', fontWeight: 600 }}>
          {conflicts.length} Schedule {conflicts.length === 1 ? 'Conflict' : 'Conflicts'} Detected
        </h4>
      </div>
      <ul style={{ margin: 0, paddingLeft: '24px', color: '#B91C1C', fontSize: '14px' }}>
        {conflicts.map((conflict, idx) => (
          <li key={idx} style={{ marginBottom: '4px' }}>
            {conflict.description || conflict.reason || 'Overlapping activity or vendor time slot.'}
          </li>
        ))}
      </ul>
    </div>
  );
};