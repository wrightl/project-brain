import { RoleGuard } from '@/_components/auth/role-guard';
import CoachSubscriptionManagement from './_components/coach-subscription-management';

export default async function CoachSubscriptionPage() {
    return (
        <RoleGuard allowedRoles={['coach']}>
            <CoachSubscriptionManagement />
        </RoleGuard>
    );
}

