import { Metadata } from 'next';
import AdminDashboard from './_components/admin-dashboard';

export const metadata: Metadata = {
    title: 'Admin Dashboard',
    description: 'Manage users and system settings',
};

export default function AdminDashboardPage() {
    return <AdminDashboard />;
}
