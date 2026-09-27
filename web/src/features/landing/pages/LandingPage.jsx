import { useCallback, useEffect, useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
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
import { tokens } from '../../../shared/theme/tokens';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import PublicNavbar from '../../../shared/components/layout/PublicNavbar';
import StorysetIllustration from '../components/StorysetIllustration';
import { storysetIllustrations } from '../components/storysetIllustrations';
import { fetchMobileAppDownloadUrl } from '../api/mobileDownloadConfig';
import { hasAdminWebAccess } from '../../../shared/auth/roleAccess';

const plannerBenefits = [
  'Generate a first-draft event plan with AI recommendations',
  'Build practical budgets and event timelines',
  'Discover vendors and organize planning tasks',
  'Coordinate approvals and keep decisions visible',
  'Track progress from the first idea through event day',
];

const vendorBenefits = [
  'Gain visibility with planners looking for event services',
  'Find relevant opportunities to participate in events',
  'Showcase services, past work, and expertise',
  'Engage directly with planners about event requirements',
  'Manage service details and quotation workflows',
];

const processSteps = [
  {
    number: '01',
    title: 'Open the mobile app',
    description: 'Create an event-planner or vendor account in the mobile app.',
    icon: <GroupsRoundedIcon />,
    color: tokens.colors.pastelPinkLight,
  },
  {
    number: '02',
    title: 'Plan or join events',
    description: 'Start an event, build a plan, or discover an opportunity.',
    icon: <CalendarMonthRoundedIcon />,
    color: tokens.colors.pastelYellowLight,
  },
  {
    number: '03',
    title: 'Collaborate and deliver',
    description: 'Coordinate details together and follow progress to the finish.',
    icon: <TaskAltRoundedIcon />,
    color: tokens.colors.pastelGreenLight,
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
          <CheckCircleRoundedIcon sx={{ color: tokens.colors.pastelGreenText, fontSize: 21, mt: '1px' }} />
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

  return (
    <Box sx={{ minHeight: '100vh', overflow: 'hidden', bgcolor: 'background.default' }}>
      <PublicNavbar />
      {token && user && hasAdminWebAccess(user.roles) && (
        <Box sx={{ bgcolor: tokens.colors.pastelPinkLight, py: 1.5, borderBottom: `1px solid ${tokens.colors.pastelPinkBorder}` }}>
          <Container maxWidth="lg">
            <Stack direction="row" spacing={2} sx={{ alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap' }}>
              <Typography variant="body2" sx={{ fontWeight: 600, color: tokens.colors.pastelPinkText }}>
                You are currently signed in as <strong>{user.email}</strong> ({user.roles?.join(', ') || 'User'}).
              </Typography>
              <Button
                variant="contained"
                size="small"
                onClick={() => navigate('/admin/dashboard')}
                sx={{
                  bgcolor: tokens.colors.pastelBlue,
                  color: tokens.colors.pastelBlueText,
                  borderRadius: 9999,
                  px: 2.5,
                  fontSize: '0.8rem',
                }}
              >
                Go to Admin Dashboard
              </Button>
            </Stack>
          </Container>
        </Box>
      )}

      <Box
        component="main"
        sx={{
          backgroundColor: tokens.colors.canvas,
        }}
      >
        <Container maxWidth="lg">
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', md: '1fr 0.95fr' },
              alignItems: 'center',
              gap: { xs: 2, md: 6 },
              py: { xs: 6, sm: 9, md: 11 },
              minHeight: { md: 600 },
            }}
          >
            <Stack spacing={2.5} sx={{ maxWidth: 640, zIndex: 1 }}>
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
                <Box component="span" sx={{ color: tokens.colors.pastelBlueText }}>
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
                Plan It connects event planners and service vendors in a unified workspace.
                Generate intelligent AI event plans, organize schedules and budgets, discover
                verified vendors, and collaborate seamlessly.
              </Typography>
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
                {['Plan', 'Collaborate', 'Deliver'].map((label, index) => (
                  <Paper
                    key={label}
                    variant="outlined"
                    sx={{
                      px: 1.4,
                      py: 0.7,
                      borderRadius: 9999,
                      bgcolor: [tokens.colors.pastelPinkLight, tokens.colors.pastelYellowLight, tokens.colors.pastelGreenLight][index],
                      borderColor: tokens.colors.borderLight,
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
                  bgcolor: tokens.colors.pastelYellowLight,
                  border: `1px solid ${tokens.colors.borderLight}`,
                }}
              />
              <StorysetIllustration
                name="teamwork"
                alt="A team collaborating around a shared project"
                sx={{ position: 'relative', width: '100%', maxHeight: 480, zIndex: 1 }}
              />
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
                  <Box sx={{ color: tokens.colors.pastelBlueText, display: 'flex' }}>{icon}</Box>
                  <Typography variant="body2" sx={{ fontWeight: 800, color: 'text.primary' }}>{label}</Typography>
                </Stack>
              ))}
            </Box>
          </SurfaceCard>
        </Container>
      </Box>

      <Container id="about" maxWidth="lg" sx={{ py: { xs: 7, md: 10 }, scrollMarginTop: 88 }}>
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
            tint={tokens.colors.pastelPinkLight}
          />
          <BenefitCard
            title="For vendors"
            description="Build stronger relationships and make it easier for the right clients to find you."
            items={vendorBenefits}
            icon={<StorefrontRoundedIcon />}
            illustration="mobile"
            imageAlt="A business owner promoting services on a mobile device"
            tint={tokens.colors.pastelBlueLight}
          />
        </Box>
      </Container>

      {/* AI-Powered Event Planning & Vendor Discovery Section */}
      <Container id="features" maxWidth="lg" sx={{ pb: { xs: 7, md: 10 }, scrollMarginTop: 88 }}>
        <SectionHeading
          eyebrow="Intelligent Event Coordination"
          title="AI-Powered Event Planning & Vendor Discovery"
          description="Let our specialized AI engines formulate the perfect draft plan while connecting you with verified local vendors."
        />
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
            gap: 3,
            mt: 4,
          }}
        >
          <SurfaceCard variant="aiPlan" sx={{ p: 3.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
              <Box sx={{ p: 1, borderRadius: 2, bgcolor: tokens.colors.pastelLavender, color: tokens.colors.pastelLavenderText }}>
                <AutoAwesomeRoundedIcon />
              </Box>
              <Typography variant="h5" sx={{ fontWeight: 800 }}>
                Intelligent AI Event Planner
              </Typography>
            </Box>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2.5, lineHeight: 1.7 }}>
              Turn your event concept into an actionable master plan in seconds with automated breakdown of schedules, budget limits, and risk prevention.
            </Typography>
            <Stack spacing={1.5}>
              {[
                'Smart Timeline Generation with conflict detection',
                'Dynamic Category Budget Allocation',
                'Risk Analysis & Recommended Mitigations',
                'Tailored Vendor Category Suggestions',
                'Smart suggestions and approval workflows',
              ].map((feat) => (
                <Stack key={feat} direction="row" spacing={1.2} sx={{ alignItems: 'center' }}>
                  <CheckCircleRoundedIcon sx={{ color: tokens.colors.pastelGreenText, fontSize: 18 }} />
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>{feat}</Typography>
                </Stack>
              ))}
            </Stack>
          </SurfaceCard>

          <SurfaceCard variant="vendor" sx={{ p: 3.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
              <Box sx={{ p: 1, borderRadius: 2, bgcolor: tokens.colors.pastelPink, color: tokens.colors.pastelPinkText }}>
                <StorefrontRoundedIcon />
              </Box>
              <Typography variant="h5" sx={{ fontWeight: 800 }}>
                Vendor Marketplace & Quotations
              </Typography>
            </Box>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2.5, lineHeight: 1.7 }}>
              Discover verified event suppliers, review curated portfolios, request quotations directly, and track responses seamlessly in one place.
            </Typography>
            <Stack spacing={1.5}>
              {[
                'Direct quotation requests with event specifics',
                'Transparent pricing types & custom terms',
                'Verified vendor business profiles & showcase galleries',
                'Real-time booking and availability calendar management',
                'Matching capabilities based on event needs',
              ].map((feat) => (
                <Stack key={feat} direction="row" spacing={1.2} sx={{ alignItems: 'center' }}>
                  <CheckCircleRoundedIcon sx={{ color: tokens.colors.pastelGreenText, fontSize: 18 }} />
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>{feat}</Typography>
                </Stack>
              ))}
            </Stack>
          </SurfaceCard>
        </Box>
      </Container>

      <Box sx={{ bgcolor: tokens.colors.cardCream, py: { xs: 7, md: 9 } }}>
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
                  borderColor: tokens.colors.borderLight,
                  boxShadow: tokens.shadows.soft,
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
                        color: tokens.colors.textPrimary,
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

      <Container id="download-app" maxWidth="lg" sx={{ py: { xs: 7, md: 10 }, scrollMarginTop: 88 }}>
        <Paper
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: '1.2fr 0.8fr' },
            alignItems: 'center',
            gap: 2,
            p: { xs: 3, sm: 5, md: 6 },
            overflow: 'hidden',
            bgcolor: tokens.colors.pastelBlueLight,
            color: tokens.colors.pastelBlueText,
            borderRadius: { xs: 4, md: 6 },
          }}
        >
          <Stack spacing={2} sx={{ alignItems: 'flex-start' }}>
            <Typography variant="overline" sx={{ color: tokens.colors.pastelBlueText, fontWeight: 800 }}>
              Your next event starts here
            </Typography>
            <Typography
              variant="h2"
              sx={{ color: tokens.colors.pastelBlueText, fontSize: { xs: '2rem', sm: '2.75rem' }, maxWidth: 620 }}
            >
              Bring the whole plan together.
            </Typography>
            <Typography sx={{ color: tokens.colors.pastelBlueText, maxWidth: 560, lineHeight: 1.7 }}>
              Download the mobile app to keep event details close, collaborate with
              your team, and move your plans forward wherever you are.
            </Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ alignItems: { xs: 'stretch', sm: 'center' } }}>
              <Button
                variant="contained"
                component="a"
                href="#download-app"
                sx={{
                  bgcolor: tokens.colors.pastelBlue,
                  color: tokens.colors.pastelBlueText,
                  borderRadius: 9999,
                  px: 3,
                  py: 1.4,
                  fontWeight: 800,
                  '&:hover': { bgcolor: tokens.colors.pastelBlueBorder },
                }}
              >
                Get Started
              </Button>
              <DownloadAppButton
                download={download}
                sx={{
                  bgcolor: 'transparent',
                  color: tokens.colors.pastelBlueText,
                  border: `1px solid ${tokens.colors.pastelBlueBorder}`,
                  '&:hover': { bgcolor: tokens.colors.surface },
                }}
              />
            </Stack>
          </Stack>
          <StorysetIllustration
            name="mobile"
            alt="Mobile tools helping a team stay connected"
            sx={{ width: '100%', maxHeight: 300, justifySelf: 'center' }}
          />
        </Paper>
      </Container>

      <Box component="footer" sx={{ borderTop: `1px solid ${tokens.colors.borderLight}`, py: 3 }}>
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
              Plan It — Event Planning Platform
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
      <Typography variant="overline" sx={{ color: tokens.colors.pastelBlueText, fontWeight: 800, letterSpacing: '0.1em' }}>
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
          <Box sx={{ color: tokens.colors.pastelBlueText, display: 'flex' }}>{icon}</Box>
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
