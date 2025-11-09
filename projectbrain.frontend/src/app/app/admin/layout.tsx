import { RoleGuard } from '@/_components/auth/role-guard';
import DashboardNav from '@/_components/dashboard/dashboard-nav';
import { getUserRoles } from '@/_lib/auth';
import { UserService } from '@/_services/user-service';
import { Metadata } from 'next';

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
    const user = await UserService.getCurrentUser();
    const roles = await getUserRoles();

    return (
        <RoleGuard allowedRoles={['admin']} redirectTo="/app">
            <div className="min-h-screen bg-gray-50">
                <DashboardNav user={user} role={roles?.[0] || null} />
                <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                    {children}
                </main>
            </div>
        </RoleGuard>
    );
}
