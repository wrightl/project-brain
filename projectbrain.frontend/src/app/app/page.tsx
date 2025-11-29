import { auth0 } from '@/_lib/auth';
import { UserService } from '@/_services/user-service';
import { redirect } from 'next/navigation';
import { SessionExpiredError } from '@/_lib/backend-api';

// Force dynamic rendering to allow access to request-time APIs
export const dynamic = 'force-dynamic';

/**
 * Main dashboard route - redirects users to their role-specific dashboard
 * or to onboarding if not yet onboarded
 */
export default async function DashboardPage() {
    try {
        // Check if user is onboarded
        const user = await UserService.getCurrentUser();
        const session = await auth0.getSession();
        // const roles = await getUserRoles();

        const role = user?.roles?.[0];

        if (!session) {
            redirect('/auth/login?returnTo=/app');
        } else if (!role) {
            redirect('/app/onboarding/select-role');
        } else if (!user?.isOnboarded) {
            redirect(`/app/onboarding/${user.roles[0].toLowerCase()}`);
        } else {
            redirect(`/app/${user.roles[0].toLowerCase()}`);
        }
    } catch (error) {
        if (error instanceof SessionExpiredError) {
            redirect('/auth/login?returnTo=/app');
        }
        throw error;
    }
}
