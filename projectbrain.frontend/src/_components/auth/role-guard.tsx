import { redirect } from 'next/navigation';
import { getUserRoles } from '@/_lib/auth';
import { UserRole } from '@/_lib/types';

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
    const userRole = await getUserRoles();

    if (!userRole || !allowedRoles.includes(userRole[0])) {
        redirect(redirectTo);
    }

    return <>{children}</>;
}
