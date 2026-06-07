import { RoleGuard } from '@/_components/auth/role-guard';
import CoachSubscriptionManagement from './_components/coach-subscription-management';

import { AppRoles } from '@/_lib/roles';
export default async function CoachSubscriptionPage() {
    return (
        <RoleGuard allowedRoles={[AppRoles.Coach]}>
            <CoachSubscriptionManagement />
        </RoleGuard>
    );
}

