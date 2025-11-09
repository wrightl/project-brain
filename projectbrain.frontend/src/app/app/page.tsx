import { UserService } from '@/_services/user-service';
import { redirect } from 'next/navigation';

// Force dynamic rendering to allow access to request-time APIs
export const dynamic = 'force-dynamic';

/**
 * Main dashboard route - redirects users to their role-specific dashboard
 * or to onboarding if not yet onboarded
 */
export default async function DashboardPage() {
    // Check if user is onboarded
    const user = await UserService.getCurrentUser();
    // const roles = await getUserRoles();

    const role = user?.roles?.[0];

    if (!user) {
        redirect('auth/login');
    } else if (!role) {
        redirect('/app/onboarding/select-role');
    } else if (!user.isOnboarded) {
        redirect(`/app/onboarding/${user.roles[0].toLowerCase()}`);
    } else {
        redirect(`/app/${user.roles[0].toLowerCase()}`);
    }
}
