import { createTheme } from '@mui/material/styles';
import { tokens } from './tokens';

export const pastelTheme = createTheme({
  palette: {
    background: {
      default: tokens.colors.canvas,
      paper: tokens.colors.surface,
    },
    primary: {
      main: tokens.colors.pastelBlue,
      contrastText: tokens.colors.pastelBlueText,
    },
    secondary: {
      main: tokens.colors.pastelPink,
      contrastText: tokens.colors.pastelPinkText,
    },
    success: {
      main: tokens.colors.pastelGreen,
      contrastText: tokens.colors.pastelGreenText,
    },
    warning: {
      main: tokens.colors.pastelPeach,
      contrastText: tokens.colors.pastelPeachText,
    },
    info: {
      main: tokens.colors.pastelLavender,
      contrastText: tokens.colors.pastelLavenderText,
    },
    text: {
      primary: tokens.colors.textPrimary,
      secondary: tokens.colors.textSecondary,
    },
    pastel: tokens.colors,
  },
  typography: {
    fontFamily: '"Plus Jakarta Sans", "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
    h1: { fontWeight: 800, letterSpacing: '-0.04em' },
    h2: { fontWeight: 700, letterSpacing: '-0.03em' },
    h3: { fontWeight: 700, letterSpacing: '-0.025em' },
    h4: {
      fontWeight: 700,
      letterSpacing: '-0.02em',
      color: tokens.colors.textPrimary,
    },
    h5: {
      fontWeight: 600,
      letterSpacing: '-0.01em',
      color: tokens.colors.textPrimary,
    },
    h6: {
      fontWeight: 600,
      fontSize: '1.05rem',
      color: tokens.colors.textPrimary,
    },
    subtitle1: {
      color: tokens.colors.textSecondary,
      fontSize: '0.95rem',
    },
    subtitle2: {
      color: tokens.colors.textMuted,
      fontSize: '0.85rem',
    },
    body1: {
      fontSize: '0.925rem',
      color: tokens.colors.textPrimary,
    },
    body2: {
      fontSize: '0.825rem',
      color: tokens.colors.textSecondary,
    },
    button: {
      textTransform: 'none',
      fontWeight: 600,
      letterSpacing: '0.01em',
    },
  },
  shape: {
    borderRadius: 20,
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          backgroundColor: tokens.colors.canvas,
          color: tokens.colors.textPrimary,
          fontFamily: '"Plus Jakarta Sans", "Inter", sans-serif',
          margin: 0,
          padding: 0,
        },
        '*': {
          boxSizing: 'border-box',
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: tokens.radius.pill,
          padding: '10px 22px',
          minHeight: 44,
          boxShadow: 'none',
          textTransform: 'none',
          fontWeight: 700,
          fontSize: '0.875rem',
          letterSpacing: '0.01em',
          whiteSpace: 'nowrap',
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: '8px',
          transition: 'all 0.2s ease',
          '&:hover': {
            boxShadow: tokens.shadows.soft,
            transform: 'translateY(-1px)',
          },
        },
        containedPrimary: {
          backgroundColor: tokens.colors.pastelBlue,
          color: tokens.colors.pastelBlueText,
          '&:hover': {
            backgroundColor: tokens.colors.pastelBlueBorder,
          },
        },
        containedSecondary: {
          backgroundColor: tokens.colors.pastelPink,
          color: tokens.colors.pastelPinkText,
          '&:hover': {
            backgroundColor: tokens.colors.pastelPinkBorder,
          },
        },
        containedSuccess: {
          backgroundColor: tokens.colors.pastelGreen,
          color: tokens.colors.pastelGreenText,
          '&:hover': { backgroundColor: tokens.colors.pastelGreenBorder },
        },
        containedInfo: {
          backgroundColor: tokens.colors.pastelLavender,
          color: tokens.colors.pastelLavenderText,
          '&:hover': { backgroundColor: tokens.colors.pastelLavenderBorder },
        },
        containedWarning: {
          backgroundColor: tokens.colors.pastelPeach,
          color: tokens.colors.pastelPeachText,
          '&:hover': { backgroundColor: tokens.colors.pastelPeachBorder },
        },
        outlined: {
          borderColor: tokens.colors.borderLight,
          color: tokens.colors.textPrimary,
          backgroundColor: tokens.colors.surface,
          '&:hover': {
            backgroundColor: tokens.colors.surface,
            borderColor: tokens.colors.pastelBlueBorder,
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: tokens.radius.card,
          boxShadow: tokens.shadows.card,
          border: `1px solid ${tokens.colors.borderLight}`,
          backgroundColor: tokens.colors.surface,
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          borderRadius: tokens.radius.card,
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: 9999,
          fontWeight: 700,
          fontSize: '0.77rem',
          height: 28,
        },
      },
    },
    MuiTextField: {
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            borderRadius: 16,
            backgroundColor: tokens.colors.surface,
            '& fieldset': {
              borderColor: tokens.colors.borderLight,
            },
            '&:hover fieldset': {
              borderColor: tokens.colors.pastelBlueBorder,
            },
            '&.Mui-focused fieldset': {
              borderColor: tokens.colors.pastelBlueBorder,
              borderWidth: 1.4,
            },
          },
        },
      },
    },
    MuiSelect: {
      styleOverrides: {
        select: {
          backgroundColor: tokens.colors.surface,
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        head: {
          color: tokens.colors.textSecondary,
          fontWeight: 700,
          fontSize: '0.76rem',
          textTransform: 'uppercase',
          letterSpacing: '0.05em',
          backgroundColor: tokens.colors.canvas,
        },
        body: {
          fontSize: '0.9rem',
          color: tokens.colors.textPrimary,
        },
      },
    },
  },
});
