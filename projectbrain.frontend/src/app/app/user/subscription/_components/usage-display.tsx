'use client';

import { useSubscription, useUsage } from '@/_hooks/queries/use-subscription';
import UsageMeter from '@/_components/usage-meter';
import { SkeletonCard } from '@/_components/ui/skeleton';

export default function UsageDisplay() {
    const { data: subscription, isLoading: subscriptionLoading, error: subscriptionError } = useSubscription();
    const { data: usage, isLoading: usageLoading, error: usageError } = useUsage();

    const loading = subscriptionLoading || usageLoading;
    const error = subscriptionError || usageError;

    if (loading) {
        return <SkeletonCard />;
    }

    if (error) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="text-red-600">
                    Error: {error instanceof Error ? error.message : 'Failed to load usage data'}
                </div>
            </div>
        );
    }

    if (!usage || !subscription) return null;

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
