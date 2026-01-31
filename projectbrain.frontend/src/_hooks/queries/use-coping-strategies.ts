import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/_lib/api-client';

export type CopingStrategyLibraryItem = {
    id: string;
    title: string;
    description: string;
    iconKey?: string | null;
    savedAt: string;
};

export const copingStrategyKeys = {
    all: ['copingStrategies'] as const,
    library: () => [...copingStrategyKeys.all, 'library'] as const,
};

export function useCopingStrategyLibrary() {
    return useQuery({
        queryKey: copingStrategyKeys.library(),
        queryFn: async () => {
            return await apiClient<{ items: CopingStrategyLibraryItem[] }>(
                '/api/coping-strategies/library',
            );
        },
        staleTime: 2 * 60 * 1000,
    });
}
