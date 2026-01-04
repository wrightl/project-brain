import { redirect } from 'next/navigation';
import { getUserRoles } from '@/_lib/auth';
import { UserRole } from '@/_lib/types';
import { SessionExpiredError } from '@/_lib/backend-api';

interface RoleGuardProps {
    children: React.ReactNode;
    allowedRoles: UserRole[];
    redirectTo?: string;
}

/**
 * Server Component that guards routes based on user role
 */
export async function RoleGuard({
    children,
    allowedRoles,
    redirectTo = '/app',
}: RoleGuardProps) {
    try {
        const userRole = await getUserRoles();

        console.log('userRole', userRole);

        if (!userRole || !allowedRoles.includes(userRole[0])) {
            redirect(redirectTo);
        }

        return <>{children}</>;
    } catch (error) {
        if (error instanceof SessionExpiredError) {
            redirect('/auth/login?returnTo=' + encodeURIComponent(redirectTo));
        }
        throw error;
    }
}
