import { RoleGuard } from '@/_components/auth/role-guard';
import { getUserRoles } from '@/_lib/auth';
import DashboardNav from '@/_components/dashboard/dashboard-nav';
import { User } from '@/_lib/types';
import { Metadata } from 'next';
import { UserService } from '@/_services/user-service';
import { SessionExpiredError } from '@/_lib/backend-api';
import { redirect } from 'next/navigation';
import TimezoneInitializer from './_components/timezone-initializer';

import { AppRoles } from '@/_lib/roles';
export const metadata: Metadata = {
    title: 'User',
    description: 'User dashboard and features',
};

// Force dynamic rendering to allow access to request-time APIs
export const dynamic = 'force-dynamic';

export default async function UserLayout({
    children,
}: {
    children: React.ReactNode;
}) {
    try {
        const user = await UserService.getCurrentUser();
        const roles = await getUserRoles();

        return (
            <RoleGuard allowedRoles={[AppRoles.User]} redirectTo="/app">
                <div className="min-h-screen bg-gray-50">
                    <DashboardNav user={user as User | null} role={roles?.[0] ?? null} />
                    <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                        <TimezoneInitializer />
                        {children}
                    </main>
                </div>
            </RoleGuard>
        );
    } catch (error) {
        if (error instanceof SessionExpiredError) {
            redirect('/auth/login?returnTo=/app/user');
        }
        throw error;
    }
}
