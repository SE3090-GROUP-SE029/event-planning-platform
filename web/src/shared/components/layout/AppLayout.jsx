import { Box } from '@mui/material';
import { useAuthStore } from '../../store/authStore';
import CollapsibleSidebar from './CollapsibleSidebar';
import TopSearchNavbar from './TopSearchNavbar';
import { tokens } from '../../theme/tokens';

export default function AppLayout({
  activeTab,
  title,
  subtitle,
  onSearch,
  searchValue,
  children,
}) {
  const user = useAuthStore((state) => state.user);

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: tokens.colors.canvas, p: { xs: 1.5, md: 2.5 } }}>
      <Box sx={{ display: 'flex', gap: { xs: 2, md: 3 }, minHeight: 'calc(100vh - 32px)' }}>
        <CollapsibleSidebar activeTab={activeTab} />
        <Box sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
          <TopSearchNavbar
            user={user}
            title={title}
            subtitle={subtitle}
            onSearch={onSearch}
            searchValue={searchValue}
          />
          <Box sx={{ flex: 1 }}>{children}</Box>
        </Box>
      </Box>
    </Box>
  );
}
