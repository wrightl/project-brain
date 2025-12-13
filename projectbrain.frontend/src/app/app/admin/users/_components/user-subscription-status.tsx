'use client';

import { useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

interface UserSubscriptionStatusProps {
    userId: string;
}

interface SubscriptionData {
    tier: string;
    status: string;
    trialEndsAt?: string;
    currentPeriodEnd?: string;
    canceledAt?: string;
    isExcluded?: boolean;
}

export default function UserSubscriptionStatus({ userId }: UserSubscriptionStatusProps) {
    const [subscription, setSubscription] = useState<SubscriptionData | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        loadSubscription();
    }, [userId]);

    const loadSubscription = async () => {
        try {
            const response = await fetchWithAuth(`/api/admin/users/${userId}/subscription`);
            if (response.ok) {
                const data = await response.json();
                setSubscription(data);
            }
        } catch (err) {
            console.error('Failed to load subscription:', err);
        } finally {
            setLoading(false);
        }
    };

    if (loading) {
        return <span className="text-xs text-gray-400">Loading...</span>;
    }

    if (!subscription) {
        return <span className="text-xs text-gray-500">No data</span>;
    }

    const isActive = subscription.status === 'active' || subscription.status === 'trialing';
    const isExcluded = subscription.isExcluded === true;

    return (
        <div className="space-y-1">
            <div className="flex items-center gap-2">
                <span className="text-xs font-medium capitalize">{subscription.tier}</span>
                <span
                    className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${
                        isActive
                            ? 'bg-green-100 text-green-800'
                            : subscription.status === 'canceled'
                            ? 'bg-yellow-100 text-yellow-800'
                            : 'bg-gray-100 text-gray-800'
                    }`}
                >
                    {subscription.status}
                </span>
                {isExcluded && (
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                        Excluded
                    </span>
                )}
            </div>
            {isActive && subscription.currentPeriodEnd && (
                <div className="text-xs text-gray-500">
                    Expires: {new Date(subscription.currentPeriodEnd).toLocaleDateString()}
                </div>
            )}
            {subscription.status === 'trialing' && subscription.trialEndsAt && (
                <div className="text-xs text-gray-500">
                    Trial ends: {new Date(subscription.trialEndsAt).toLocaleDateString()}
                </div>
            )}
            {!isActive && subscription.canceledAt && (
                <div className="text-xs text-gray-500">
                    Expired: {new Date(subscription.canceledAt).toLocaleDateString()}
                </div>
            )}
        </div>
    );
}

