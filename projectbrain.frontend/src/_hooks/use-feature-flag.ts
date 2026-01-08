'use client';

import { useEffect, useState } from 'react';
import { getFlags, FeatureFlags } from '@/_lib/flags';

/**
 * Hook to check if a specific feature flag is enabled
 */
export function useFeatureFlag(flagName: keyof FeatureFlags): boolean {
    const [enabled, setEnabled] = useState(false);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        getFlags()
            .then((flags) => {
                setEnabled(flags[flagName] ?? false);
                setLoading(false);
            })
            .catch((error) => {
                console.error('Error fetching feature flags:', error);
                setEnabled(false); // Safe default: disabled
                setLoading(false);
            });
    }, [flagName]);

    return enabled;
}

/**
 * Hook specifically for checking if agent feature is enabled
 */
export function useAgentFeatureEnabled(): boolean {
    return useFeatureFlag('AgentFeatureEnabled');
}
