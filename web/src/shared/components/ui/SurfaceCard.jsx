import { Card } from '@mui/material';
import { tokens } from '../../theme/tokens';

const variantStyles = {
  analytics: {
    backgroundColor: tokens.colors.pastelBlueLight,
    border: `1px solid ${tokens.colors.pastelBlueBorder}`,
  },
  aiPlan: {
    backgroundColor: tokens.colors.pastelLavenderLight,
    border: `1px solid ${tokens.colors.pastelLavenderBorder}`,
  },
  task: {
    backgroundColor: tokens.colors.pastelGreenLight,
    border: `1px solid ${tokens.colors.pastelGreenBorder}`,
  },
  vendor: {
    backgroundColor: tokens.colors.pastelPinkLight,
    border: `1px solid ${tokens.colors.pastelPinkBorder}`,
  },
  budget: {
    backgroundColor: tokens.colors.pastelPeachLight,
    border: `1px solid ${tokens.colors.pastelPeachBorder}`,
  },
  default: {
    backgroundColor: tokens.colors.surface,
    border: `1px solid ${tokens.colors.borderLight}`,
  },
};

export default function SurfaceCard({ children, variant = 'default', sx = {}, ...props }) {
  const chosenVariant = variantStyles[variant] || variantStyles.default;

  return (
    <Card
      {...props}
      sx={{
        borderRadius: tokens.radius.card,
        boxShadow: tokens.shadows.soft,
        transition: 'transform 0.2s ease, box-shadow 0.2s ease',
        ...chosenVariant,
        ...sx,
      }}
    >
      {children}
    </Card>
  );
}
