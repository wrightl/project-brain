'use client';

import { useEffect, useState } from 'react';
import { getFlags, FeatureFlags } from '@/_lib/flags';

export type FeatureFlagState = {
    enabled: boolean;
    loading: boolean;
};

/**
 * Hook to check if a specific feature flag is enabled, including load state.
 * Wait for `loading === false` before treating `enabled === false` as a deny.
 */
export function useFeatureFlagState(
    flagName: keyof FeatureFlags,
): FeatureFlagState {
    const [enabled, setEnabled] = useState(false);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        let cancelled = false;
        getFlags()
            .then((flags) => {
                if (cancelled) return;
                setEnabled(flags[flagName] ?? false);
                setLoading(false);
            })
            .catch((error) => {
                console.error('Error fetching feature flags:', error);
                if (cancelled) return;
                setEnabled(false); // Safe default: disabled
                setLoading(false);
            });
        return () => {
            cancelled = true;
        };
    }, [flagName]);

    return { enabled, loading };
}

/**
 * Hook to check if a specific feature flag is enabled
 */
export function useFeatureFlag(flagName: keyof FeatureFlags): boolean {
    return useFeatureFlagState(flagName).enabled;
}

/**
 * Hook specifically for checking if agent feature is enabled
 */
export function useAgentFeatureEnabled(): boolean {
    return useFeatureFlag('AgentFeatureEnabled');
}

/**
 * Hook specifically for checking if the in-app Community hub is enabled
 */
export function useCommunityFeatureEnabled(): boolean {
    return useFeatureFlag('CommunityFeatureEnabled');
}

/**
 * Community hub flag with loading state (avoid redirecting before flags resolve)
 */
export function useCommunityFeatureFlag(): FeatureFlagState {
    return useFeatureFlagState('CommunityFeatureEnabled');
}
