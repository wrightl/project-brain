'use client';

import { useState } from 'react';
import CoachTierComparison from './coach-tier-comparison';
import CurrentSubscription from '@/app/app/user/subscription/_components/current-subscription';
import UsageDisplay from '@/app/app/user/subscription/_components/usage-display';

export default function CoachSubscriptionManagement() {
    const [refreshKey, setRefreshKey] = useState(0);

    const handleUpdate = () => {
        // Force child components to refresh by changing key
        setRefreshKey((prev) => prev + 1);
    };

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
            <h1 className="text-3xl font-bold mb-8">Subscription Management</h1>

            <div className="space-y-8">
                <CurrentSubscription
                    key={`current-${refreshKey}`}
                    onUpdate={handleUpdate}
                />
                <UsageDisplay key={`usage-${refreshKey}`} />
                <CoachTierComparison
                    key={`tier-${refreshKey}`}
                    onUpgrade={handleUpdate}
                />
            </div>
        </div>
    );
}
