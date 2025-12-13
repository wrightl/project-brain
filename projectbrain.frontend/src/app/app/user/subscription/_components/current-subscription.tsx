'use client';

import { useState, useEffect } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { Subscription } from '@/_services/subscription-service';
import toast from 'react-hot-toast';
import ConfirmationDialog from '@/_components/confirmation-dialog';

interface CurrentSubscriptionProps {
    onUpdate?: () => void;
}

export default function CurrentSubscription({ onUpdate }: CurrentSubscriptionProps) {
    const [subscription, setSubscription] = useState<Subscription | null>(null);
    const [loading, setLoading] = useState(true);
    const [canceling, setCanceling] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false);

    useEffect(() => {
        loadSubscription();
    }, []);

    const loadSubscription = async () => {
        try {
            setLoading(true);
            const response = await fetchWithAuth('/api/subscriptions/me');
            
            if (!response.ok) {
                throw new Error('Failed to fetch subscription');
            }

            const data: Subscription = await response.json();
            setSubscription(data);
        } catch (err) {
            console.error('Failed to load subscription:', err);
            setError(err instanceof Error ? err.message : 'Failed to load subscription');
        } finally {
            setLoading(false);
        }
    };

    const handleCancelClick = () => {
        setCancelConfirmOpen(true);
    };

    const handleCancel = async () => {
        try {
            setCanceling(true);
            setError(null);
            
            const response = await fetchWithAuth('/api/subscriptions/cancel', {
                method: 'POST',
            });

            if (!response.ok) {
                throw new Error('Failed to cancel subscription');
            }

            toast.success('Subscription canceled successfully');
            await loadSubscription(); // Reload subscription data
            onUpdate?.(); // Call parent update if provided
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to cancel subscription');
            toast.error(err instanceof Error ? err.message : 'Failed to cancel subscription');
        } finally {
            setCanceling(false);
            setCancelConfirmOpen(false);
        }
    };

    const formatDate = (dateString?: string) => {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString();
    };

    if (loading) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="animate-pulse">
                    <div className="h-6 bg-gray-200 rounded w-1/4 mb-4"></div>
                    <div className="space-y-2">
                        <div className="h-4 bg-gray-200 rounded w-1/2"></div>
                        <div className="h-4 bg-gray-200 rounded w-1/3"></div>
                    </div>
                </div>
            </div>
        );
    }

    if (error && !subscription) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="text-red-600">Error: {error}</div>
            </div>
        );
    }

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
                            onClick={handleCancelClick}
                            disabled={canceling}
                            className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50"
                        >
                            {canceling ? 'Canceling...' : 'Cancel Subscription'}
                        </button>
                        {error && <p className="text-red-600 mt-2">{error}</p>}
                    </div>
                )}
            </div>

            {/* Cancel Subscription Confirmation Dialog */}
            <ConfirmationDialog
                isOpen={cancelConfirmOpen}
                onClose={() => setCancelConfirmOpen(false)}
                onConfirm={handleCancel}
                title="Cancel Subscription"
                message="Are you sure you want to cancel your subscription? You will lose access to premium features at the end of your billing period."
                confirmText="Cancel Subscription"
                cancelText="Keep Subscription"
                variant="warning"
            />
        </div>
    );
}

