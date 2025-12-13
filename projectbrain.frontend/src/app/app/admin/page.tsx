import dynamicImport from 'next/dynamic';
import { Metadata } from 'next';
import { SkeletonCard } from '@/_components/ui/skeleton';

const AdminDashboard = dynamicImport(() => import('./_components/admin-dashboard'), {
    loading: () => (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
            <SkeletonCard />
        </div>
    ),
});

export const metadata: Metadata = {
    title: 'Admin Dashboard',
    description: 'Manage users and system settings',
};

export default function AdminDashboardPage() {
    return <AdminDashboard />;
}
