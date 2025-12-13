'use client';

import { useEffect, useState, ReactNode } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import Link from 'next/link';

interface FeatureGateProps {
    feature: string;
    children: ReactNode;
    fallback?: ReactNode;
    showUpgradePrompt?: boolean;
}

export default function FeatureGate({
    feature,
    children,
    fallback,
    showUpgradePrompt = true,
}: FeatureGateProps) {
    const [allowed, setAllowed] = useState<boolean | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        checkFeature();
    }, []);

    const checkFeature = async () => {
        try {
            const response = await fetchWithAuth('/api/subscriptions/tier');

            if (!response.ok) {
                throw new Error('Failed to fetch tier information');
            }

            const data: { tier: string; userType: string } =
                await response.json();

            // Check if feature is allowed based on tier
            const featureAllowed = checkFeatureAccess(data.tier, feature);
            setAllowed(featureAllowed);
        } catch (error) {
            console.error('Error checking feature access:', error);
            setAllowed(false);
        } finally {
            setLoading(false);
        }
    };

    const checkFeatureAccess = (tier: string, featureName: string): boolean => {
        switch (featureName) {
            case 'speech_input':
                return tier !== 'Free';
            case 'external_integrations':
                return tier === 'Ultimate';
            case 'research_reports':
                return tier !== 'Free';
            default:
                return true;
        }
    };

    if (loading) {
        return null; // Or a loading spinner
    }

    if (!allowed) {
        if (fallback) {
            return <>{fallback}</>;
        }

        if (showUpgradePrompt) {
            return (
                <div className="p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                    <p className="text-sm text-yellow-800 mb-2">
                        This feature is only available in Pro and Ultimate
                        tiers.
                    </p>
                    <Link
                        href="/app/user/subscription"
                        className="text-sm text-yellow-900 underline font-medium"
                    >
                        Upgrade now →
                    </Link>
                </div>
            );
        }

        return null;
    }

    return <>{children}</>;
}
