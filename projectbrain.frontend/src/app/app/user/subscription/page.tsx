import { RoleGuard } from '@/_components/auth/role-guard';
import SubscriptionManagement from './_components/subscription-management';

export default async function SubscriptionPage() {
    return (
        <RoleGuard allowedRoles={['user']}>
            <SubscriptionManagement />
        </RoleGuard>
    );
}

