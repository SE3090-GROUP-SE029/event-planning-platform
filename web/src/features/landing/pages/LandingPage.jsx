import { useCallback, useEffect, useState } from 'react';
import { Link as RouterLink, Navigate, useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Container,
  Link,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded';
import AutoAwesomeRoundedIcon from '@mui/icons-material/AutoAwesomeRounded';
import CalendarMonthRoundedIcon from '@mui/icons-material/CalendarMonthRounded';
import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded';
import GroupsRoundedIcon from '@mui/icons-material/GroupsRounded';
import InsightsRoundedIcon from '@mui/icons-material/InsightsRounded';
import StorefrontRoundedIcon from '@mui/icons-material/StorefrontRounded';
import TaskAltRoundedIcon from '@mui/icons-material/TaskAltRounded';
import { useAuthStore } from '../../../shared/store/authStore';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StorysetIllustration from '../components/StorysetIllustration';
import { storysetIllustrations } from '../components/storysetIllustrations';
import { fetchMobileAppDownloadUrl } from '../api/mobileDownloadConfig';

const plannerBenefits = [
  'Create and manage every event in one place',
  'Generate a first-draft plan with AI',
  'Keep budgets, schedules, and tasks organized',
  'Find and collaborate with trusted vendors',
  'Track progress from kickoff through completion',
];

const vendorBenefits = [
  'Receive relevant event opportunities',
  'Manage booking requests in one workspace',
  'Showcase services, work, and expertise',
  'Communicate directly with event planners',
  'Track customer engagements from inquiry to booking',
  'Build visibility and lasting customer relationships',
];

const processSteps = [
  {
    number: '01',
    title: 'Create an account',
    description: 'Set up a planner or vendor profile in a few simple steps.',
    icon: <GroupsRoundedIcon />,
    color: '#FDEEF5',
  },
  {
    number: '02',
    title: 'Plan or join events',
    description: 'Start an event, build a plan, or discover an opportunity.',
    icon: <CalendarMonthRoundedIcon />,
    color: '#FEF8E4',
  },
  {
    number: '03',
    title: 'Collaborate and deliver',
    description: 'Coordinate details together and follow progress to the finish.',
    icon: <TaskAltRoundedIcon />,
    color: '#F0F6EC',
  },
];

function useMobileAppDownload() {
  const [state, setState] = useState({ status: 'loading', url: null });
  const [attempt, setAttempt] = useState(0);
  const retry = useCallback(() => {
    setState({ status: 'loading', url: null });
    setAttempt((value) => value + 1);
  }, []);

  useEffect(() => {
    let active = true;

    fetchMobileAppDownloadUrl()
      .then((url) => {
        if (active) {
          setState({ status: url ? 'available' : 'unavailable', url });
        }
      })
      .catch((error) => {
        console.error('Unable to load mobile app download configuration.', error);
        if (active) {
          setState({ status: 'error', url: null });
        }
      });

    return () => {
      active = false;
    };
  }, [attempt]);

  return { ...state, retry };
}

function DownloadAppButton({ download, size = 'large', sx = {} }) {
  const isLoading = download.status === 'loading';
  const isAvailable = download.status === 'available';
  const label = isLoading
    ? 'Loading download link'
    : 'Download Mobile App';

  return (
    <Stack spacing={0.75} sx={{ alignItems: 'flex-start' }}>
      <Button
        component={isAvailable ? 'a' : 'button'}
        href={isAvailable ? download.url : undefined}
        target={isAvailable ? '_blank' : undefined}
        rel={isAvailable ? 'noopener noreferrer' : undefined}
        variant="contained"
        color="primary"
        size={size}
        disabled={!isAvailable}
        endIcon={isLoading ? <CircularProgress size={18} color="inherit" /> : <ArrowForwardRoundedIcon />}
        sx={{ borderRadius: 9999, px: 3, minHeight: 48, ...sx }}
      >
        {label}
      </Button>
      {download.status === 'unavailable' && (
        <Typography variant="caption" color="text.secondary">
          The mobile app download link will be available soon.
        </Typography>
      )}
      {download.status === 'error' && (
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
          <Typography variant="caption" color="error">
            The download link could not be loaded.
          </Typography>
          <Button size="small" onClick={download.retry} sx={{ minWidth: 0, p: 0.5 }}>
            Retry
          </Button>
        </Stack>
      )}
    </Stack>
  );
}

function BenefitList({ items }) {
  return (
    <Stack component="ul" spacing={1.35} sx={{ listStyle: 'none', m: 0, p: 0 }}>
      {items.map((item) => (
        <Stack
          key={item}
          component="li"
          direction="row"
          spacing={1.2}
          sx={{ alignItems: 'flex-start' }}
        >
          <CheckCircleRoundedIcon sx={{ color: '#738B52', fontSize: 21, mt: '1px' }} />
          <Typography variant="body1">{item}</Typography>
        </Stack>
      ))}
    </Stack>
  );
}

export default function LandingPage() {
  const token = useAuthStore((state) => state.accessToken);
  const user = useAuthStore((state) => state.user);
  const navigate = useNavigate();
  const download = useMobileAppDownload();

  if (token && user) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <Box sx={{ minHeight: '100vh', overflow: 'hidden', bgcolor: 'background.default' }}>
      <Box
        component="header"
        sx={{
          position: 'sticky',
          top: 0,
          zIndex: 10,
          bgcolor: 'rgba(247, 243, 233, 0.92)',
          backdropFilter: 'blur(16px)',
          borderBottom: '1px solid rgba(25, 25, 28, 0.06)',
        }}
      >
        <Container maxWidth="lg">
          <Stack
            direction="row"
            sx={{ minHeight: 76, alignItems: 'center', justifyContent: 'space-between' }}
          >
            <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
              <Box
                aria-hidden="true"
                sx={{
                  width: 42,
                  height: 42,
                  display: 'grid',
                  placeItems: 'center',
                  borderRadius: 3,
                  bgcolor: '#F9BFD8',
                  color: '#19191C',
                }}
              >
                <CalendarMonthRoundedIcon />
              </Box>
              <Typography variant="h6" sx={{ fontWeight: 800, letterSpacing: '-0.03em' }}>
                Plan It
              </Typography>
            </Stack>
            <Stack
              direction="row"
              spacing={{ xs: 0.5, sm: 1.5 }}
              sx={{ alignItems: 'center' }}
            >
              <Button component={RouterLink} to="/login" color="inherit" sx={{ fontWeight: 700 }}>
                Sign in
              </Button>
              <Button
                component={RouterLink}
                to="/login"
                variant="contained"
                sx={{ px: { xs: 1.75, sm: 2.5 } }}
              >
                Get started
              </Button>
            </Stack>
          </Stack>
        </Container>
      </Box>

      <Box
        component="main"
        sx={{
          background:
            'radial-gradient(ellipse at 82% 16%, rgba(249,191,216,0.32), transparent 33%), linear-gradient(180deg, #FBF8F0 0%, #F7F3E9 100%)',
        }}
      >
        <Container maxWidth="lg">
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', md: '1fr 0.95fr' },
              alignItems: 'center',
              gap: { xs: 2, md: 6 },
              py: { xs: 6, sm: 9, md: 12 },
              minHeight: { md: 620 },
            }}
          >
            <Stack spacing={2.5} sx={{ maxWidth: 640, zIndex: 1 }}>
              <Paper
                variant="outlined"
                sx={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  alignSelf: 'flex-start',
                  gap: 0.8,
                  borderRadius: 9999,
                  px: 1.5,
                  py: 0.7,
                  bgcolor: '#F0F6EC',
                  borderColor: 'rgba(115, 139, 82, 0.2)',
                }}
              >
                <AutoAwesomeRoundedIcon sx={{ color: '#738B52', fontSize: 17 }} />
                <Typography variant="caption" sx={{ color: '#40552C', fontWeight: 800 }}>
                  Make every detail feel effortless
                </Typography>
              </Paper>
              <Typography
                component="h1"
                variant="h1"
                sx={{
                  fontSize: { xs: '2.65rem', sm: '3.5rem', md: '4.25rem' },
                  lineHeight: { xs: 1.08, md: 1.03 },
                  maxWidth: 650,
                }}
              >
                Event planning,{' '}
                <Box component="span" sx={{ color: '#8C694B' }}>
                  all in one place.
                </Box>
              </Typography>
              <Typography
                variant="h6"
                sx={{
                  maxWidth: 590,
                  color: 'text.secondary',
                  fontWeight: 500,
                  lineHeight: 1.65,
                }}
              >
                Event Planning Platform brings planners and vendors together to
                organize events, follow progress, and create AI-assisted plans
                with confidence.
              </Typography>
              <Stack
                direction={{ xs: 'column', sm: 'row' }}
                spacing={1.5}
                sx={{ alignItems: { xs: 'stretch', sm: 'center' } }}
              >
                <DownloadAppButton download={download} />
                <Button
                  variant="outlined"
                  onClick={() => navigate('/login')}
                  endIcon={<ArrowForwardRoundedIcon />}
                  sx={{ borderRadius: 9999, px: 2.75, minHeight: 48 }}
                >
                  Get started
                </Button>
              </Stack>
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
                {['Plan', 'Collaborate', 'Deliver'].map((label, index) => (
                  <Paper
                    key={label}
                    variant="outlined"
                    sx={{
                      px: 1.4,
                      py: 0.7,
                      borderRadius: 9999,
                      bgcolor: ['#FDEEF5', '#FEF8E4', '#F0F6EC'][index],
                      borderColor: 'rgba(25, 25, 28, 0.05)',
                    }}
                  >
                    <Typography variant="caption" sx={{ fontWeight: 800 }}>
                      {label}
                    </Typography>
                  </Paper>
                ))}
              </Stack>
            </Stack>

            <Box
              sx={{
                position: 'relative',
                minHeight: { xs: 320, sm: 420, md: 510 },
                display: 'grid',
                placeItems: 'center',
              }}
            >
              <Box
                aria-hidden="true"
                sx={{
                  position: 'absolute',
                  width: { xs: 280, sm: 400, md: 460 },
                  aspectRatio: '1',
                  borderRadius: '50%',
                  bgcolor: '#FEF8E4',
                  border: '1px solid rgba(25, 25, 28, 0.05)',
                }}
              />
              <StorysetIllustration
                name="teamwork"
                alt="A team collaborating around a shared project"
                sx={{ position: 'relative', width: '100%', maxHeight: 480, zIndex: 1 }}
              />
              <Paper
                elevation={0}
                sx={{
                  position: 'absolute',
                  top: { xs: 12, sm: 28 },
                  left: { xs: 0, sm: -12 },
                  p: 1.5,
                  borderRadius: 3,
                  boxShadow: '0 12px 32px rgba(40, 35, 26, 0.1)',
                  zIndex: 2,
                }}
              >
                <Stack direction="row" spacing={1.1} sx={{ alignItems: 'center' }}>
                  <AutoAwesomeRoundedIcon sx={{ color: '#8C694B' }} />
                  <Box>
                    <Typography variant="caption" color="text.secondary">Planning assistant</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 800 }}>A clear plan, faster</Typography>
                  </Box>
                </Stack>
              </Paper>
              <Paper
                elevation={0}
                sx={{
                  position: 'absolute',
                  right: { xs: 0, sm: -4 },
                  bottom: { xs: 10, sm: 34 },
                  p: 1.5,
                  borderRadius: 3,
                  boxShadow: '0 12px 32px rgba(40, 35, 26, 0.1)',
                  zIndex: 2,
                }}
              >
                <Stack direction="row" spacing={1.1} sx={{ alignItems: 'center' }}>
                  <CheckCircleRoundedIcon sx={{ color: '#738B52' }} />
                  <Box>
                    <Typography variant="caption" color="text.secondary">Event progress</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 800 }}>Every detail in sync</Typography>
                  </Box>
                </Stack>
              </Paper>
            </Box>
          </Box>
        </Container>

        <Container maxWidth="lg" sx={{ pb: { xs: 6, md: 9 } }}>
          <SurfaceCard sx={{ p: { xs: 2.5, md: 3.5 } }}>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr 1fr', sm: 'repeat(4, 1fr)' },
                gap: 2,
              }}
            >
              {[
                ['Plan with clarity', <CalendarMonthRoundedIcon key="calendar" />],
                ['Stay on budget', <InsightsRoundedIcon key="insights" />],
                ['Work as a team', <GroupsRoundedIcon key="groups" />],
                ['Grow your business', <StorefrontRoundedIcon key="store" />],
              ].map(([label, icon]) => (
                <Stack
                  key={label}
                  direction="row"
                  spacing={1}
                  sx={{ alignItems: 'center', justifyContent: 'center' }}
                >
                  <Box sx={{ color: '#8C694B', display: 'flex' }}>{icon}</Box>
                  <Typography variant="body2" sx={{ fontWeight: 800, color: 'text.primary' }}>{label}</Typography>
                </Stack>
              ))}
            </Box>
          </SurfaceCard>
        </Container>
      </Box>

      <Container id="benefits" maxWidth="lg" sx={{ py: { xs: 7, md: 10 } }}>
        <SectionHeading
          eyebrow="Made for both sides of the celebration"
          title="A better way to work together"
          description="One thoughtful workspace for the people bringing every event to life."
        />
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
            gap: 2.5,
            mt: 4,
          }}
        >
          <BenefitCard
            title="For event planners"
            description="Spend less time juggling details and more time creating an event worth remembering."
            items={plannerBenefits}
            icon={<CalendarMonthRoundedIcon />}
            illustration="planning"
            imageAlt="A team putting together a detailed business plan"
            tint="#FDEEF5"
          />
          <BenefitCard
            title="For vendors"
            description="Build stronger relationships and make it easier for the right clients to find you."
            items={vendorBenefits}
            icon={<StorefrontRoundedIcon />}
            illustration="mobile"
            imageAlt="A business owner promoting services on a mobile device"
            tint="#EEF5FC"
          />
        </Box>
      </Container>

      <Box sx={{ bgcolor: '#FAF7EF', py: { xs: 7, md: 9 } }}>
        <Container id="how-it-works" maxWidth="lg">
          <SectionHeading
            eyebrow="Simple from the start"
            title="How it works"
            description="A few clear steps from first idea to a well-coordinated event."
          />
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' },
              gap: 2,
              mt: 4,
            }}
          >
            {processSteps.map((step) => (
              <Card
                key={step.number}
                variant="outlined"
                sx={{
                  height: '100%',
                  borderRadius: 4,
                  borderColor: 'rgba(25, 25, 28, 0.07)',
                  boxShadow: '0 8px 24px rgba(28, 25, 20, 0.04)',
                }}
              >
                <CardContent sx={{ p: { xs: 2.5, md: 3 }, '&:last-child': { pb: { xs: 2.5, md: 3 } } }}>
                  <Stack
                    direction="row"
                    sx={{ mb: 3, justifyContent: 'space-between', alignItems: 'center' }}
                  >
                    <Box
                      sx={{
                        width: 54,
                        height: 54,
                        borderRadius: 3,
                        bgcolor: step.color,
                        display: 'grid',
                        placeItems: 'center',
                        color: '#19191C',
                      }}
                    >
                      {step.icon}
                    </Box>
                    <Typography variant="overline" sx={{ fontWeight: 800, color: 'text.secondary' }}>
                      STEP {step.number}
                    </Typography>
                  </Stack>
                  <Typography variant="h5" sx={{ fontWeight: 800, mb: 1 }}>
                    {step.title}
                  </Typography>
                  <Typography color="text.secondary" sx={{ lineHeight: 1.7 }}>
                    {step.description}
                  </Typography>
                </CardContent>
              </Card>
            ))}
          </Box>
        </Container>
      </Box>

      <Container maxWidth="lg" sx={{ py: { xs: 7, md: 10 } }}>
        <Paper
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: '1.2fr 0.8fr' },
            alignItems: 'center',
            gap: 2,
            p: { xs: 3, sm: 5, md: 6 },
            overflow: 'hidden',
            bgcolor: '#19191C',
            color: '#FFFFFF',
            borderRadius: { xs: 4, md: 6 },
          }}
        >
          <Stack spacing={2} sx={{ alignItems: 'flex-start' }}>
            <Typography variant="overline" sx={{ color: '#F9BFD8', fontWeight: 800 }}>
              Your next event starts here
            </Typography>
            <Typography
              variant="h2"
              sx={{ color: '#FFFFFF', fontSize: { xs: '2rem', sm: '2.75rem' }, maxWidth: 620 }}
            >
              Bring the whole plan together.
            </Typography>
            <Typography sx={{ color: 'rgba(255,255,255,0.72)', maxWidth: 560, lineHeight: 1.7 }}>
              Download the mobile app to keep event details close, collaborate with
              your team, and move your plans forward wherever you are.
            </Typography>
            <DownloadAppButton
              download={download}
              sx={{
                bgcolor: '#F9BFD8',
                color: '#19191C',
                '&:hover': { bgcolor: '#F7AFCF' },
                '&.Mui-disabled': { bgcolor: 'rgba(249,191,216,0.28)', color: 'rgba(255,255,255,0.6)' },
              }}
            />
          </Stack>
          <StorysetIllustration
            name="mobile"
            alt="Mobile tools helping a team stay connected"
            sx={{ width: '100%', maxHeight: 300, justifySelf: 'center' }}
          />
        </Paper>
      </Container>

      <Box component="footer" sx={{ borderTop: '1px solid rgba(25, 25, 28, 0.08)', py: 3 }}>
        <Container maxWidth="lg">
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={1.5}
            sx={{
              justifyContent: 'space-between',
              alignItems: { xs: 'flex-start', sm: 'center' },
            }}
          >
            <Typography variant="body2" sx={{ fontWeight: 800, color: 'text.primary' }}>
              Event Planning Platform
            </Typography>
            <Stack direction="row" spacing={2}>
              <Link component={RouterLink} to="/login" color="text.secondary" underline="hover" variant="body2">
                Sign in
              </Link>
              <Link
                href={storysetIllustrations.teamwork.source}
                target="_blank"
                rel="noopener noreferrer"
                color="text.secondary"
                underline="hover"
                variant="body2"
              >
                Illustrations by Storyset
              </Link>
            </Stack>
          </Stack>
        </Container>
      </Box>
    </Box>
  );
}

function SectionHeading({ eyebrow, title, description }) {
  return (
    <Stack
      spacing={1}
      sx={{ maxWidth: 680, mx: 'auto', alignItems: 'center', textAlign: 'center' }}
    >
      <Typography variant="overline" sx={{ color: '#7A6344', fontWeight: 800, letterSpacing: '0.1em' }}>
        {eyebrow}
      </Typography>
      <Typography variant="h2" sx={{ fontSize: { xs: '2rem', sm: '2.65rem' } }}>
        {title}
      </Typography>
      <Typography color="text.secondary" sx={{ lineHeight: 1.7 }}>
        {description}
      </Typography>
    </Stack>
  );
}

function BenefitCard({ title, description, items, icon, illustration, imageAlt, tint }) {
  return (
    <SurfaceCard sx={{ p: { xs: 2.5, sm: 3.5 }, height: '100%' }}>
      <Stack spacing={2.5} sx={{ height: '100%' }}>
        <Box
          sx={{
            minHeight: 180,
            borderRadius: 4,
            bgcolor: tint,
            display: 'grid',
            placeItems: 'center',
            overflow: 'hidden',
          }}
        >
          <StorysetIllustration
            name={illustration}
            alt={imageAlt}
            sx={{ width: '100%', height: 200, objectFit: 'contain' }}
          />
        </Box>
        <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
          <Box sx={{ color: '#8C694B', display: 'flex' }}>{icon}</Box>
          <Typography variant="h4" sx={{ fontWeight: 800, fontSize: '1.3rem' }}>
            {title}
          </Typography>
        </Stack>
        <Typography color="text.secondary" sx={{ lineHeight: 1.7 }}>
          {description}
        </Typography>
        <BenefitList items={items} />
      </Stack>
    </SurfaceCard>
  );
}
