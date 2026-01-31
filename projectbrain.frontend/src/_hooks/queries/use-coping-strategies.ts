import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/_lib/api-client';

export type CopingStrategyLibraryItem = {
    id: string;
    title: string;
    description: string;
    iconKey?: string | null;
    rating?: number | null;
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
                '/api/strategies/library',
            );
        },
        staleTime: 2 * 60 * 1000,
    });
}

export function useDeleteCopingStrategy() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (id: string) => {
            return apiClient(`/api/strategies/${id}`, { method: 'DELETE' });
        },
        onSuccess: () => {
            queryClient.invalidateQueries({
                queryKey: copingStrategyKeys.library(),
            });
        },
    });
}

export function useRateCopingStrategy() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (args: { id: string; rating: number }) => {
            return apiClient(`/api/strategies/${args.id}`, {
                method: 'PUT',
                body: { rating: args.rating },
            });
        },
        onSuccess: () => {
            queryClient.invalidateQueries({
                queryKey: copingStrategyKeys.library(),
            });
        },
    });
}
