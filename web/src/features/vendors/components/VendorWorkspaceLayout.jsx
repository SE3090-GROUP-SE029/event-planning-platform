import AppLayout from '../../../shared/components/layout/AppLayout';

export default function VendorWorkspaceLayout({ activeTab, title, subtitle, children }) {
  return (
    <AppLayout activeTab={activeTab} title={title} subtitle={subtitle}>
      {children}
    </AppLayout>
  );
}

