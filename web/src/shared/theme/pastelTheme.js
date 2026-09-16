import { createTheme } from '@mui/material/styles';
import { tokens } from './tokens';

export const pastelTheme = createTheme({
  palette: {
    background: {
      default: tokens.colors.canvas,
      paper: tokens.colors.surface,
    },
    primary: {
      main: tokens.colors.obsidian,
      contrastText: '#FFFFFF',
    },
    secondary: {
      main: tokens.colors.pastelPink,
      contrastText: tokens.colors.pastelPinkText,
    },
    text: {
      primary: tokens.colors.textPrimary,
      secondary: tokens.colors.textSecondary,
    },
    pastel: tokens.colors,
  },
  typography: {
    fontFamily: '"Plus Jakarta Sans", "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
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
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 9999,
          padding: '8px 20px',
          boxShadow: 'none',
          '&:hover': {
            boxShadow: '0 4px 14px rgba(25, 25, 28, 0.12)',
          },
        },
        containedPrimary: {
          backgroundColor: tokens.colors.obsidian,
          color: '#FFFFFF',
          '&:hover': {
            backgroundColor: tokens.colors.obsidianHover,
          },
        },
        outlined: {
          borderColor: tokens.colors.borderLight,
          color: tokens.colors.textPrimary,
          '&:hover': {
            backgroundColor: 'rgba(0, 0, 0, 0.04)',
            borderColor: tokens.colors.obsidian,
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 20,
          boxShadow: tokens.shadows.card,
          border: 'none',
          backgroundColor: '#FFFFFF',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          borderRadius: 20,
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: 9999,
          fontWeight: 600,
          fontSize: '0.775rem',
        },
      },
    },
    MuiTextField: {
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            borderRadius: 14,
            backgroundColor: '#FFFFFF',
            '& fieldset': {
              borderColor: 'rgba(0,0,0,0.08)',
            },
            '&:hover fieldset': {
              borderColor: tokens.colors.obsidian,
            },
          },
        },
      },
    },
  },
});
