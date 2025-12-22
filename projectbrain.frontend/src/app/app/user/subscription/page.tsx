import { Metadata } from 'next';
import { RoleGuard } from '@/_components/auth/role-guard';
import SubscriptionManagement from './_components/subscription-management';
import SubscriptionSummary from '../profile/_components/subscription-summary';

export const metadata: Metadata = {
    title: 'Subscription',
    description: 'Manage your subscription and billing',
};

export default async function SubscriptionPage() {
    return (
        <RoleGuard allowedRoles={['user']}>
            <div className="space-y-6">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900">
                        Subscription
                    </h1>
                    <p className="mt-2 text-sm text-gray-600">
                        Manage your subscription plan and billing
                    </p>
                </div>
                <SubscriptionSummary />
                <SubscriptionManagement />
            </div>
        </RoleGuard>
    );
}

