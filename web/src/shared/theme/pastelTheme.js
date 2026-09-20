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
          borderRadius: 9999,
          padding: '10px 20px',
          boxShadow: 'none',
          textTransform: 'none',
          fontWeight: 700,
          '&:hover': {
            boxShadow: '0 8px 18px rgba(25, 25, 28, 0.12)',
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
          borderColor: 'rgba(25, 25, 28, 0.12)',
          color: tokens.colors.textPrimary,
          '&:hover': {
            backgroundColor: 'rgba(25, 25, 28, 0.04)',
            borderColor: tokens.colors.obsidian,
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 24,
          boxShadow: tokens.shadows.card,
          border: '1px solid rgba(25, 25, 28, 0.05)',
          backgroundColor: '#FFFFFF',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          borderRadius: 24,
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
            backgroundColor: '#FFFFFF',
            '& fieldset': {
              borderColor: 'rgba(18, 17, 20, 0.12)',
            },
            '&:hover fieldset': {
              borderColor: tokens.colors.obsidian,
            },
            '&.Mui-focused fieldset': {
              borderColor: tokens.colors.obsidian,
              borderWidth: 1.4,
            },
          },
        },
      },
    },
    MuiSelect: {
      styleOverrides: {
        select: {
          backgroundColor: '#FFFFFF',
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
          backgroundColor: 'rgba(247, 243, 233, 0.8)',
        },
        body: {
          fontSize: '0.9rem',
          color: tokens.colors.textPrimary,
        },
      },
    },
  },
});
