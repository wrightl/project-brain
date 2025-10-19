import AdminDashboard from '@/components/dashboard/admin-dashboard';
import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Admin Dashboard',
  description: 'Manage users and system settings',
};

export default function AdminDashboardPage() {
  return <AdminDashboard />;
}
