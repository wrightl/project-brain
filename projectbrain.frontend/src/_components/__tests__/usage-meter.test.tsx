import { render, screen } from '@/_lib/test-utils';
import UsageMeter from '@/_components/usage-meter';

describe('UsageMeter', () => {
    it('renders label and current value', () => {
        render(<UsageMeter label="Test Usage" current={50} limit={100} />);
        expect(screen.getByText('Test Usage')).toBeInTheDocument();
        expect(screen.getByText('50')).toBeInTheDocument();
    });

    it('displays percentage correctly', () => {
        render(<UsageMeter label="Test" current={25} limit={100} />);
        const progressBar = screen.getByRole('progressbar');
        expect(progressBar).toHaveAttribute('aria-valuenow', '25');
        expect(progressBar).toHaveAttribute('aria-valuemax', '100');
    });

    it('handles unlimited usage (limit of -1)', () => {
        render(<UsageMeter label="Unlimited" current={1000} limit={-1} />);
        expect(screen.getByText('1000')).toBeInTheDocument();
        expect(screen.queryByText('/ -1')).not.toBeInTheDocument();
    });

    it('displays unit when provided', () => {
        render(<UsageMeter label="Storage" current={50} limit={100} unit="MB" />);
        expect(screen.getByText(/50.*MB/i)).toBeInTheDocument();
    });
});

