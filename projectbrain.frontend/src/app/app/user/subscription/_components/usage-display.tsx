'use client';

import { useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { Usage, Subscription } from '@/_services/subscription-service';
import UsageMeter from '@/_components/usage-meter';

export default function UsageDisplay() {
    const [usage, setUsage] = useState<Usage | null>(null);
    const [subscription, setSubscription] = useState<Subscription | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        loadData();
    }, []);

    const loadData = async () => {
        try {
            setLoading(true);
            const [usageResponse, subscriptionResponse] = await Promise.all([
                fetchWithAuth('/api/subscriptions/usage'),
                fetchWithAuth('/api/subscriptions/me'),
            ]);

            if (!usageResponse.ok) {
                throw new Error('Failed to fetch usage data');
            }
            if (!subscriptionResponse.ok) {
                throw new Error('Failed to fetch subscription data');
            }

            const [usageData, subscriptionData] = await Promise.all([
                usageResponse.json() as Promise<Usage>,
                subscriptionResponse.json() as Promise<Subscription>,
            ]);

            setUsage(usageData);
            setSubscription(subscriptionData);
        } catch (err) {
            console.error('Failed to load usage data:', err);
            setError(
                err instanceof Error ? err.message : 'Failed to load usage data'
            );
        } finally {
            setLoading(false);
        }
    };

    if (loading) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="animate-pulse">
                    <div className="h-6 bg-gray-200 rounded w-1/4 mb-4"></div>
                    <div className="space-y-4">
                        <div className="h-4 bg-gray-200 rounded w-3/4"></div>
                        <div className="h-4 bg-gray-200 rounded w-1/2"></div>
                    </div>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="text-red-600">Error: {error}</div>
            </div>
        );
    }

    if (!usage) return null;

    const tier = subscription?.tier || 'Free';
    const userType = subscription?.userType || 'user';

    // Safely extract usage values with defaults
    const aiQueriesDaily: number = usage.aiQueries?.daily ?? 0;
    const aiQueriesMonthly: number = usage.aiQueries?.monthly ?? 0;
    const coachMessagesMonthly: number = usage.coachMessages?.monthly ?? 0;
    const fileStorageMB: number = usage.fileStorage?.megabytes ?? 0;
    const researchReportsMonthly: number = usage.researchReports?.monthly ?? 0;
    const clientMessagesMonthly: number = usage.clientMessages?.monthly ?? 0;

    // Get limits based on tier
    const getLimits = (): {
        dailyAIQueries?: number;
        monthlyAIQueries?: number;
        coachConnections?: number;
        coachMessages?: number;
        files?: number;
        fileStorageMB?: number;
        researchReports?: number;
        clientConnections?: number;
        clientMessages?: number;
    } => {
        if (userType === 'coach') {
            return {
                clientConnections: tier === 'Pro' ? -1 : 3,
                clientMessages: tier === 'Pro' ? -1 : 10,
            };
        } else {
            return {
                dailyAIQueries: tier === 'Free' ? 50 : -1,
                monthlyAIQueries: tier === 'Free' ? 200 : -1,
                coachConnections: tier === 'Free' ? 3 : -1,
                coachMessages: tier === 'Free' ? 200 : -1,
                files: tier === 'Free' ? 20 : tier === 'Pro' ? -1 : -1,
                fileStorageMB:
                    tier === 'Free' ? 100 : tier === 'Pro' ? 500 : -1,
                researchReports:
                    tier === 'Pro' ? 1 : tier === 'Ultimate' ? -1 : 0,
            };
        }
    };

    const limits = getLimits();

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <h2 className="text-2xl font-semibold mb-4">Usage</h2>

            <div className="space-y-6">
                {userType === 'user' && (
                    <>
                        <div>
                            <h3 className="font-medium mb-2">AI Queries</h3>
                            <div className="space-y-2">
                                <UsageMeter
                                    label="Daily"
                                    current={aiQueriesDaily}
                                    limit={limits.dailyAIQueries ?? -1}
                                />
                                <UsageMeter
                                    label="Monthly"
                                    current={aiQueriesMonthly}
                                    limit={limits.monthlyAIQueries ?? -1}
                                />
                            </div>
                        </div>

                        <div>
                            <h3 className="font-medium mb-2">Coach Messages</h3>
                            <UsageMeter
                                label="Monthly"
                                current={coachMessagesMonthly}
                                limit={limits.coachMessages ?? -1}
                            />
                        </div>

                        <div>
                            <h3 className="font-medium mb-2">File Storage</h3>
                            <UsageMeter
                                label="Storage"
                                current={Math.round(fileStorageMB)}
                                limit={limits.fileStorageMB ?? -1}
                                unit="MB"
                            />
                        </div>

                        {tier !== 'Free' && (
                            <div>
                                <h3 className="font-medium mb-2">
                                    Research Reports
                                </h3>
                                <UsageMeter
                                    label="Monthly"
                                    current={researchReportsMonthly}
                                    limit={limits.researchReports ?? -1}
                                />
                            </div>
                        )}
                    </>
                )}

                {userType === 'coach' && (
                    <>
                        <div>
                            <h3 className="font-medium mb-2">
                                Client Messages
                            </h3>
                            <UsageMeter
                                label="Monthly"
                                current={clientMessagesMonthly}
                                limit={limits.clientMessages ?? -1}
                            />
                        </div>
                    </>
                )}
            </div>
        </div>
    );
}
