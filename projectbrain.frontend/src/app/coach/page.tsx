import CoachDashboard from '@/components/dashboard/coach-dashboard';
import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Coach Dashboard',
  description: 'Manage your coaching sessions and clients',
};

export default function CoachDashboardPage() {
  return <CoachDashboard />;
}
