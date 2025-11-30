'use client';

import { useEffect, useState } from 'react';
import { Subscription, Usage } from '@/_services/subscription-service';
import { apiClient } from '@/_lib/api-client';
import TierComparison from './tier-comparison';
import CurrentSubscription from './current-subscription';
import UsageDisplay from './usage-display';

export default function SubscriptionManagement() {
    const [subscription, setSubscription] = useState<Subscription | null>(null);
    const [usage, setUsage] = useState<Usage | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        loadData();
    }, []);

    const loadData = async () => {
        try {
            setLoading(true);
            const [subData, usageData] = await Promise.all([
                apiClient<Subscription>('/api/subscriptions/me'),
                apiClient<Usage>('/api/subscriptions/usage'),
            ]);
            setSubscription(subData);
            setUsage(usageData);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to load subscription data');
        } finally {
            setLoading(false);
        }
    };

    if (loading) {
        return (
            <div className="flex items-center justify-center min-h-screen">
                <div className="text-lg">Loading subscription information...</div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="flex items-center justify-center min-h-screen">
                <div className="text-red-600">Error: {error}</div>
            </div>
        );
    }

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
            <h1 className="text-3xl font-bold mb-8">Subscription Management</h1>

            <div className="space-y-8">
                <CurrentSubscription subscription={subscription} onUpdate={loadData} />
                <UsageDisplay usage={usage} subscription={subscription} />
                <TierComparison currentTier={subscription?.tier || 'Free'} onUpgrade={loadData} />
            </div>
        </div>
    );
}

