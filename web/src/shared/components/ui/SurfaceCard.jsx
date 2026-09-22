import { Card } from '@mui/material';

export default function SurfaceCard({ children, sx = {}, ...props }) {
  return (
    <Card
      {...props}
      sx={{
        borderRadius: 1,
        border: '1px solid rgba(25, 25, 28, 0.06)',
        background: 'linear-gradient(180deg, rgba(255,255,255,0.96) 0%, rgba(255,255,255,0.9) 100%)',
        boxShadow: '0 12px 28px rgba(40, 35, 26, 0.06)',
        ...sx,
      }}
    >
      {children}
    </Card>
  );
}
