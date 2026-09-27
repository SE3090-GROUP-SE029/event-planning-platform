import { renderToStaticMarkup } from 'react-dom/server';
import { describe, expect, it, vi } from 'vitest';
import VendorApprovalAction from './VendorApprovalAction';

describe('VendorApprovalAction', () => {
  it('shows an approval button only for pending vendors', () => {
    const pending = renderToStaticMarkup(
      <VendorApprovalAction status="PENDING" onApprove={vi.fn()} />
    );
    const approved = renderToStaticMarkup(
      <VendorApprovalAction status="APPROVED" onApprove={vi.fn()} />
    );
    const rejected = renderToStaticMarkup(
      <VendorApprovalAction status="REJECTED" onApprove={vi.fn()} />
    );

    expect(pending).toContain('Approve Vendor');
    expect(approved).not.toContain('Approve Vendor');
    expect(rejected).not.toContain('Approve Vendor');
  });

  it('shows progress and disables repeat approval while pending', () => {
    const markup = renderToStaticMarkup(
      <VendorApprovalAction status="PENDING" loading disabled onApprove={vi.fn()} />
    );

    expect(markup).toContain('Approving…');
    expect(markup).toContain('disabled=""');
  });
});
