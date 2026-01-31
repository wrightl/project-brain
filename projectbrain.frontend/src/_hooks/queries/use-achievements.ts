import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/_lib/api-client';

export type Achievement = {
    id: string;
    key: string;
    title: string;
    description: string;
    iconKey?: string | null;
    earnedAt?: string | null;
};

export const achievementKeys = {
    all: ['achievements'] as const,
    list: () => [...achievementKeys.all, 'list'] as const,
};

export function useAchievements() {
    return useQuery({
        queryKey: achievementKeys.list(),
        queryFn: async () => {
            return await apiClient<{ items: Achievement[] }>(
                '/api/achievements',
            );
        },
        staleTime: 2 * 60 * 1000,
    });
}
