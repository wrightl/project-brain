import { getUserRoles } from '@/_lib/auth';
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
    const roles = await getUserRoles();

    const role = roles?.[0] || 'user';

    // If user doesn't exist or isn't onboarded, redirect to onboarding
    if (!user || !user.isOnboarded) {
        switch (role) {
            case 'admin':
                redirect('/app/onboarding/admin');
            case 'coach':
                redirect('/app/onboarding/coach');
            default:
                redirect('/app/onboarding/user');
        }
    }

    // User is onboarded, redirect to role-specific dashboard
    switch (role) {
        case 'admin':
            redirect('/app/admin');
        case 'coach':
            redirect('/app/coach');
        case 'user':
            redirect('/app/user');
        default:
            redirect('/auth/login');
    }
}
