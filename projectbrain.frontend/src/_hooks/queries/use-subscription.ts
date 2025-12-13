import { useQuery } from '@tanstack/react-query';
import { Subscription, Usage } from '@/_lib/types';
import { apiClient } from '@/_lib/api-client';

export const subscriptionKeys = {
    all: ['subscriptions'] as const,
    me: () => [...subscriptionKeys.all, 'me'] as const,
    usage: () => [...subscriptionKeys.all, 'usage'] as const,
    tier: () => [...subscriptionKeys.all, 'tier'] as const,
};

export function useSubscription() {
    return useQuery<Subscription>({
        queryKey: subscriptionKeys.me(),
        queryFn: () => apiClient<Subscription>('/api/subscriptions/me'),
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useUsage() {
    return useQuery<Usage>({
        queryKey: subscriptionKeys.usage(),
        queryFn: () => apiClient<Usage>('/api/subscriptions/usage'),
        staleTime: 60 * 1000, // 1 minute
    });
}

export function useTier() {
    return useQuery<{ tier: string; userType: string }>({
        queryKey: subscriptionKeys.tier(),
        queryFn: () => apiClient<{ tier: string; userType: string }>('/api/subscriptions/tier'),
        staleTime: 5 * 60 * 1000, // 5 minutes
    });
}

