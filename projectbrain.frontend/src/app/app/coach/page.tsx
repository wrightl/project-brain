import { Metadata } from 'next';
import CoachDashboard from './_components/coach-dashboard';

export const metadata: Metadata = {
    title: 'Coach Dashboard',
    description: 'Manage your coaching sessions and clients',
};

export default function CoachDashboardPage() {
    return <CoachDashboard />;
}
