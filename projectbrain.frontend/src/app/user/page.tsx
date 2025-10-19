import UserDashboard from '@/components/dashboard/user-dashboard';
import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Dashboard',
  description: 'Your personal dashboard',
};

export default function UserDashboardPage() {
  return <UserDashboard />;
}
