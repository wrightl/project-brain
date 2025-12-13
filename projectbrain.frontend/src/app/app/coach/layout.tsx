import { RoleGuard } from '@/_components/auth/role-guard';
import { getUserRoles } from '@/_lib/auth';
import DashboardNav from '@/_components/dashboard/dashboard-nav';
import { User } from '@/_lib/types';
import { Metadata } from 'next';
import { UserService } from '@/_services/user-service';

export const metadata: Metadata = {
    title: 'Coach',
    description: 'Coach dashboard and tools',
};

// Force dynamic rendering to allow access to request-time APIs
export const dynamic = 'force-dynamic';

export default async function CoachLayout({
    children,
}: {
    children: React.ReactNode;
}) {
    const user = await UserService.getCurrentUser();
    const roles = await getUserRoles();

    return (
        <RoleGuard allowedRoles={['coach']} redirectTo="/app">
            <div className="min-h-screen bg-gray-50">
                <DashboardNav user={user as User | null} role={roles?.[0] || null} />
                <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                    {children}
                </main>
            </div>
        </RoleGuard>
    );
}
