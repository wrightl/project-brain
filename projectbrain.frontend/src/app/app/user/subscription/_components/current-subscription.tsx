'use client';

import { useState } from 'react';
import { Subscription, SubscriptionService } from '@/_services/subscription-service';

interface CurrentSubscriptionProps {
    subscription: Subscription | null;
    onUpdate: () => void;
}

export default function CurrentSubscription({ subscription, onUpdate }: CurrentSubscriptionProps) {
    const [canceling, setCanceling] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleCancel = async () => {
        if (!confirm('Are you sure you want to cancel your subscription? You will lose access to premium features at the end of your billing period.')) {
            return;
        }

        try {
            setCanceling(true);
            setError(null);
            await SubscriptionService.cancelSubscription();
            alert('Subscription canceled successfully');
            onUpdate();
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to cancel subscription');
        } finally {
            setCanceling(false);
        }
    };

    const formatDate = (dateString?: string) => {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString();
    };

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <h2 className="text-2xl font-semibold mb-4">Current Subscription</h2>
            
            <div className="space-y-4">
                <div>
                    <span className="font-medium">Tier: </span>
                    <span className="text-lg font-semibold capitalize">{subscription?.tier || 'Free'}</span>
                </div>
                
                <div>
                    <span className="font-medium">Status: </span>
                    <span className="capitalize">{subscription?.status || 'active'}</span>
                </div>

                {subscription?.trialEndsAt && (
                    <div>
                        <span className="font-medium">Trial Ends: </span>
                        <span>{formatDate(subscription.trialEndsAt)}</span>
                    </div>
                )}

                {subscription?.currentPeriodEnd && (
                    <div>
                        <span className="font-medium">Next Billing Date: </span>
                        <span>{formatDate(subscription.currentPeriodEnd)}</span>
                    </div>
                )}

                {subscription?.canceledAt && (
                    <div className="text-yellow-600">
                        <span className="font-medium">Canceled: </span>
                        <span>{formatDate(subscription.canceledAt)}</span>
                        <p className="text-sm mt-1">Your subscription will remain active until the end of your billing period.</p>
                    </div>
                )}

                {subscription && subscription.tier !== 'Free' && subscription.status === 'active' && !subscription.canceledAt && (
                    <div className="mt-4">
                        <button
                            onClick={handleCancel}
                            disabled={canceling}
                            className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50"
                        >
                            {canceling ? 'Canceling...' : 'Cancel Subscription'}
                        </button>
                        {error && <p className="text-red-600 mt-2">{error}</p>}
                    </div>
                )}
            </div>
        </div>
    );
}

