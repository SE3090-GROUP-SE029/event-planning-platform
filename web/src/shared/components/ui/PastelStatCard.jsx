import { Box, Typography } from '@mui/material';

const COLOR_MAP = {
  yellow: {
    bg: '#FEE388',
    text: '#382D03',
    subText: '#6D5B18',
    badgeBg: 'rgba(255, 255, 255, 0.45)',
    barDark: '#19191C',
    barLight: '#E8CA65',
  },
  pink: {
    bg: '#F9BFD8',
    text: '#44142D',
    subText: '#7A2C55',
    badgeBg: 'rgba(255, 255, 255, 0.45)',
    chartLine: '#9B336A',
    dot: '#19191C',
  },
  green: {
    bg: '#C4DDB8',
    text: '#1C3417',
    subText: '#42613B',
    badgeBg: 'rgba(255, 255, 255, 0.45)',
  },
  blue: {
    bg: '#BCD8F0',
    text: '#122E4D',
    subText: '#375B82',
    badgeBg: 'rgba(255, 255, 255, 0.45)',
  },
};

export default function PastelStatCard({
  variant = 'yellow',
  title,
  subtitle,
  children,
  watermark,
  sx = {},
}) {
  const c = COLOR_MAP[variant] || COLOR_MAP.yellow;

  return (
    <Box
      sx={{
        backgroundColor: c.bg,
        borderRadius: '20px',
        p: '20px',
        position: 'relative',
        overflow: 'hidden',
        minHeight: 185,
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        boxShadow: '0 4px 20px rgba(35, 25, 15, 0.03)',
        transition: 'transform 0.2s cubic-bezier(0.2, 0, 0, 1), box-shadow 0.2s',
        '&:hover': {
          transform: 'translateY(-2px)',
          boxShadow: '0 8px 26px rgba(35, 25, 15, 0.06)',
        },
        ...sx,
      }}
    >
      {/* Background Watermark Motifs */}
      {watermark && (
        <Box
          sx={{
            position: 'absolute',
            right: 12,
            top: 12,
            pointerEvents: 'none',
            opacity: 0.18,
            color: c.text,
          }}
        >
          {watermark}
        </Box>
      )}

      {/* Header */}
      <Box sx={{ zIndex: 1, mb: 1 }}>
        <Typography
          variant="h6"
          sx={{
            color: c.text,
            fontWeight: 700,
            fontSize: '1.05rem',
            lineHeight: 1.2,
          }}
        >
          {title}
        </Typography>
        {subtitle && (
          <Typography
            variant="body2"
            sx={{
              color: c.subText,
              fontSize: '0.8rem',
              mt: 0.3,
            }}
          >
            {subtitle}
          </Typography>
        )}
      </Box>

      {/* Main Content Area */}
      <Box sx={{ zIndex: 1, mt: 'auto' }}>
        {children}
      </Box>
    </Box>
  );
}
