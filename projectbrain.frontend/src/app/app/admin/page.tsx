import dynamicImport from 'next/dynamic';
import { Metadata } from 'next';
import { SkeletonCard } from '@/_components/ui/skeleton';

const AdminDashboard = dynamicImport(() => import('./_components/admin-dashboard'), {
    loading: () => <SkeletonCard />,
});

export const metadata: Metadata = {
    title: 'Admin Dashboard',
    description: 'Manage users and system settings',
};

export default function AdminDashboardPage() {
    return <AdminDashboard />;
}
