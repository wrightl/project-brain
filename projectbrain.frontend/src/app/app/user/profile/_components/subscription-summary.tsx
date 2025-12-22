'use client';

import { useEffect, useState } from 'react';
import { Subscription, Usage } from '@/_lib/types';
import { apiClient } from '@/_lib/api-client';

export default function SubscriptionSummary() {
    const [subscription, setSubscription] = useState<Subscription | null>(null);
    const [usage, setUsage] = useState<Usage | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        loadData();
    }, []);

    const loadData = async () => {
        try {
            const [subData, usageData] = await Promise.all([
                apiClient<Subscription>('/api/subscriptions/me'),
                apiClient<Usage>('/api/subscriptions/usage'),
            ]);
            setSubscription(subData);
            setUsage(usageData);
        } catch (err) {
            console.error('Failed to load subscription data:', err);
        } finally {
            setLoading(false);
        }
    };

    if (loading) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="animate-pulse">
                    <div className="h-4 bg-gray-200 rounded w-1/4 mb-4"></div>
                    <div className="h-4 bg-gray-200 rounded w-1/2"></div>
                </div>
            </div>
        );
    }

    const tier = subscription?.tier || 'Free';
    const status = subscription?.status || 'active';

    // Get tier limits
    const getLimits = () => {
        if (tier === 'Free') {
            return {
                dailyAIQueries: 50,
                monthlyAIQueries: 200,
                coachConnections: 3,
                coachMessages: 200,
                files: 20,
                fileStorageMB: 100,
            };
        } else if (tier === 'Pro') {
            return {
                dailyAIQueries: -1,
                monthlyAIQueries: -1,
                coachConnections: -1,
                coachMessages: -1,
                files: -1,
                fileStorageMB: 500,
            };
        } else {
            return {
                dailyAIQueries: -1,
                monthlyAIQueries: -1,
                coachConnections: -1,
                coachMessages: -1,
                files: -1,
                fileStorageMB: -1,
            };
        }
    };

    const limits = getLimits();

    return (
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
                    <span className="text-sm text-gray-600">Current Tier</span>
                    <span className="text-sm font-semibold text-gray-900 capitalize">
                        {tier}
                    </span>
                </div>

                <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Status</span>
                    <span className="text-sm font-medium capitalize">
                        <span
                            className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                status === 'active' || status === 'trialing'
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
                                subscription.trialEndsAt
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
                                subscription.currentPeriodEnd
                            ).toLocaleDateString()}
                        </span>
                    </div>
                )}

                {usage && (
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
                                    {Math.round(usage.fileStorage.megabytes)} MB
                                    {limits.fileStorageMB >= 0 &&
                                        ` / ${limits.fileStorageMB} MB`}
                                </span>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
