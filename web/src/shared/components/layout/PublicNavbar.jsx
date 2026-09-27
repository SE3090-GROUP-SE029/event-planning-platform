import {
  Box,
  Button,
  Container,
  Stack,
  Typography,
} from '@mui/material';
import CalendarMonthRoundedIcon from '@mui/icons-material/CalendarMonthRounded';
import { tokens } from '../../theme/tokens';

const publicLinks = [
  { label: 'Home', href: '/' },
  { label: 'Features', href: '#features' },
  { label: 'About', href: '#about' },
  { label: 'Download App', href: '#download-app' },
];

export default function PublicNavbar() {
  return (
    <Box
      component="header"
      sx={{
        position: 'sticky',
        top: 0,
        zIndex: 1100,
        bgcolor: tokens.colors.surface,
        borderBottom: `1px solid ${tokens.colors.borderLight}`,
      }}
    >
      <Container
        maxWidth="lg"
        sx={{
          minHeight: 76,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 2,
          flexWrap: 'wrap',
          py: 1,
        }}
      >
        <Box
          component="a"
          href="/"
          sx={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 1,
            color: tokens.colors.textPrimary,
            textDecoration: 'none',
            flexShrink: 0,
          }}
        >
          <Box
            sx={{
              width: 38,
              height: 38,
              borderRadius: tokens.radius.sm,
              display: 'grid',
              placeItems: 'center',
              bgcolor: tokens.colors.pastelBlueLight,
              color: tokens.colors.pastelBlueText,
            }}
          >
            <CalendarMonthRoundedIcon fontSize="small" />
          </Box>
          <Typography component="span" variant="h6" sx={{ fontWeight: 800 }}>
            Plan It
          </Typography>
        </Box>

        <Stack
          component="nav"
          aria-label="Public navigation"
          direction="row"
          spacing={{ xs: 0, sm: 0.5 }}
          useFlexGap
          sx={{ flexWrap: 'wrap', alignItems: 'center', justifyContent: 'flex-end' }}
        >
          {publicLinks.map((link) => (
            <Button
              key={link.label}
              component="a"
              href={link.href}
              color="inherit"
              sx={{
                minHeight: 40,
                px: { xs: 1, sm: 1.5 },
                fontSize: '0.875rem',
                color: tokens.colors.textSecondary,
                '&:hover': {
                  bgcolor: tokens.colors.pastelBlueLight,
                  color: tokens.colors.pastelBlueText,
                },
              }}
            >
              {link.label}
            </Button>
          ))}
          <Button
            component="a"
            href="/login"
            variant="contained"
            color="primary"
            sx={{ minHeight: 40, px: { xs: 1.5, sm: 2 } }}
          >
            Admin Login
          </Button>
        </Stack>
      </Container>
    </Box>
  );
}
