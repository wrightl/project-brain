'use client';

import { Usage, Subscription } from '@/_services/subscription-service';
import UsageMeter from '@/_components/usage-meter';

interface UsageDisplayProps {
    usage: Usage | null;
    subscription: Subscription | null;
}

export default function UsageDisplay({ usage, subscription }: UsageDisplayProps) {
    if (!usage) return null;

    const tier = subscription?.tier || 'Free';
    const userType = subscription?.userType || 'user';

    // Get limits based on tier
    const getLimits = () => {
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
                fileStorageMB: tier === 'Free' ? 100 : tier === 'Pro' ? 500 : -1,
                researchReports: tier === 'Pro' ? 1 : tier === 'Ultimate' ? -1 : 0,
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
                                    current={usage.aiQueries.daily}
                                    limit={limits.dailyAIQueries}
                                />
                                <UsageMeter
                                    label="Monthly"
                                    current={usage.aiQueries.monthly}
                                    limit={limits.monthlyAIQueries}
                                />
                            </div>
                        </div>

                        <div>
                            <h3 className="font-medium mb-2">Coach Messages</h3>
                            <UsageMeter
                                label="Monthly"
                                current={usage.coachMessages.monthly}
                                limit={limits.coachMessages}
                            />
                        </div>

                        <div>
                            <h3 className="font-medium mb-2">File Storage</h3>
                            <UsageMeter
                                label="Storage"
                                current={Math.round(usage.fileStorage.megabytes)}
                                limit={limits.fileStorageMB}
                                unit="MB"
                            />
                        </div>

                        {tier !== 'Free' && (
                            <div>
                                <h3 className="font-medium mb-2">Research Reports</h3>
                                <UsageMeter
                                    label="Monthly"
                                    current={usage.researchReports.monthly}
                                    limit={limits.researchReports}
                                />
                            </div>
                        )}
                    </>
                )}

                {userType === 'coach' && (
                    <>
                        <div>
                            <h3 className="font-medium mb-2">Client Messages</h3>
                            <UsageMeter
                                label="Monthly"
                                current={usage.clientMessages.monthly}
                                limit={limits.clientMessages}
                            />
                        </div>
                    </>
                )}
            </div>
        </div>
    );
}

