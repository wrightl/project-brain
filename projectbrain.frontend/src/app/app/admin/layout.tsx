import { RoleGuard } from '@/_components/auth/role-guard';
import { getUserRoles } from '@/_lib/auth';
import DashboardNav from '@/_components/dashboard/dashboard-nav';
import { Metadata } from 'next';
import { getCurrentUser } from '@/_lib/api-client';

export const metadata: Metadata = {
    title: 'Admin',
    description: 'Admin dashboard and management',
};

// Force dynamic rendering to allow access to request-time APIs
export const dynamic = 'force-dynamic';

export default async function AdminLayout({
    children,
}: {
    children: React.ReactNode;
}) {
    const user = await getCurrentUser();
    const roles = await getUserRoles();

    return (
        <RoleGuard allowedRoles={['admin']} redirectTo="/dashboard">
            <div className="min-h-screen bg-gray-50">
                <DashboardNav user={user} role={roles?.[0] || null} />
                <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                    {children}
                </main>
            </div>
        </RoleGuard>
    );
}
