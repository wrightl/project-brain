'use client';

import { useMemo } from 'react';
import { useSubscription, useUsage } from '@/_hooks/queries/use-subscription';
import TierComparison from './tier-comparison';
import CurrentSubscription from './current-subscription';
import UsageDisplay from './usage-display';
import ReferralsSection from './referrals-section';

export default function SubscriptionManagement() {
    const subscriptionQuery = useSubscription();
    const usageQuery = useUsage();

    const loading = subscriptionQuery.isLoading || usageQuery.isLoading;
    const error = subscriptionQuery.error || usageQuery.error;

    const subscription = subscriptionQuery.data ?? null;
    const usage = usageQuery.data ?? null;

    const refresh = async () => {
        await Promise.all([subscriptionQuery.refetch(), usageQuery.refetch()]);
    };

    const tier = subscription?.tier || 'Free';
    const status = subscription?.status || 'active';

    const limits = useMemo(() => {
        if (tier === 'Free') {
            return {
                dailyAIQueries: 50,
                monthlyAIQueries: 200,
                coachConnections: 3,
                coachMessages: 200,
                files: 20,
                fileStorageMB: 100,
            };
        }
        if (tier === 'Pro') {
            return {
                dailyAIQueries: -1,
                monthlyAIQueries: -1,
                coachConnections: -1,
                coachMessages: -1,
                files: -1,
                fileStorageMB: 500,
            };
        }
        return {
            dailyAIQueries: -1,
            monthlyAIQueries: -1,
            coachConnections: -1,
            coachMessages: -1,
            files: -1,
            fileStorageMB: -1,
        };
    }, [tier]);

    return (
        <div className="space-y-6">
            {/* SubscriptionSummary (merged) */}
            {loading ? (
                <div className="bg-white shadow rounded-lg p-6">
                    <div className="animate-pulse">
                        <div className="h-4 bg-gray-200 rounded w-1/4 mb-4"></div>
                        <div className="h-4 bg-gray-200 rounded w-1/2"></div>
                    </div>
                </div>
            ) : error ? (
                <div className="bg-white shadow rounded-lg p-6">
                    <div className="text-red-600">
                        Error:{' '}
                        {error instanceof Error
                            ? error.message
                            : 'Failed to load subscription data'}
                    </div>
                </div>
            ) : (
                <div className="bg-white shadow rounded-lg p-6">
                    <div className="mb-4">
                        <h2 className="text-lg font-semibold text-gray-900">
                            Subscription Summary
                        </h2>
                        <p className="mt-1 text-sm text-gray-600">
                            Current plan and usage
                        </p>
                    </div>

                    <div className="space-y-4">
                        <div className="flex items-center justify-between">
                            <span className="text-sm text-gray-600">
                                Current Tier
                            </span>
                            <span className="text-sm font-semibold text-gray-900 capitalize">
                                {tier}
                            </span>
                        </div>

                        <div className="flex items-center justify-between">
                            <span className="text-sm text-gray-600">
                                Status
                            </span>
                            <span className="text-sm font-medium capitalize">
                                <span
                                    className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                        status === 'active' ||
                                        status === 'trialing'
                                            ? 'bg-green-100 text-green-800'
                                            : status === 'canceled'
                                            ? 'bg-yellow-100 text-yellow-800'
                                            : 'bg-gray-100 text-gray-800'
                                    }`}
                                >
                                    {status}
                                </span>
                            </span>
                        </div>

                        {subscription?.trialEndsAt && (
                            <div className="flex items-center justify-between">
                                <span className="text-sm text-gray-600">
                                    Trial Ends
                                </span>
                                <span className="text-sm text-gray-900">
                                    {new Date(
                                        subscription.trialEndsAt,
                                    ).toLocaleDateString()}
                                </span>
                            </div>
                        )}

                        {subscription?.currentPeriodEnd && tier !== 'Free' && (
                            <div className="flex items-center justify-between">
                                <span className="text-sm text-gray-600">
                                    Next Billing
                                </span>
                                <span className="text-sm text-gray-900">
                                    {new Date(
                                        subscription.currentPeriodEnd,
                                    ).toLocaleDateString()}
                                </span>
                            </div>
                        )}

                        {/* {usage && (
                            <div className="border-t border-gray-200 pt-4 mt-4">
                                <h3 className="text-sm font-medium text-gray-900 mb-3">
                                    Usage Summary
                                </h3>
                                <div className="space-y-2">
                                    <div className="flex items-center justify-between text-sm">
                                        <span className="text-gray-600">
                                            AI Queries (Daily)
                                        </span>
                                        <span className="text-gray-900">
                                            {usage.aiQueries.daily}
                                            {limits.dailyAIQueries >= 0 &&
                                                ` / ${limits.dailyAIQueries}`}
                                        </span>
                                    </div>
                                    <div className="flex items-center justify-between text-sm">
                                        <span className="text-gray-600">
                                            AI Queries (Monthly)
                                        </span>
                                        <span className="text-gray-900">
                                            {usage.aiQueries.monthly}
                                            {limits.monthlyAIQueries >= 0 &&
                                                ` / ${limits.monthlyAIQueries}`}
                                        </span>
                                    </div>
                                    <div className="flex items-center justify-between text-sm">
                                        <span className="text-gray-600">
                                            File Storage
                                        </span>
                                        <span className="text-gray-900">
                                            {Math.round(
                                                usage.fileStorage.megabytes,
                                            )}{' '}
                                            MB
                                            {limits.fileStorageMB >= 0 &&
                                                ` / ${limits.fileStorageMB} MB`}
                                        </span>
                                    </div>
                                </div>
                            </div>
                        )} */}
                    </div>
                </div>
            )}

            {/* <CurrentSubscription onUpdate={refresh} /> */}
            <ReferralsSection subscription={subscription} />
            <UsageDisplay />
            <TierComparison currentTier={tier} onUpgrade={refresh} />
        </div>
    );
}
