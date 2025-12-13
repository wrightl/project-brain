import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Resource } from '@/_lib/types';
import { apiClient } from '@/_lib/api-client';

export const resourceKeys = {
    all: ['resources'] as const,
    lists: () => [...resourceKeys.all, 'list'] as const,
    list: (limit?: number) => [...resourceKeys.lists(), limit] as const,
    details: () => [...resourceKeys.all, 'detail'] as const,
    detail: (id: string) => [...resourceKeys.details(), id] as const,
    shared: () => [...resourceKeys.all, 'shared'] as const,
    statistics: () => [...resourceKeys.all, 'statistics'] as const,
};

export function useResources(limit?: number) {
    return useQuery<Resource[]>({
        queryKey: resourceKeys.list(limit),
        queryFn: () => {
            const queryParam = limit ? `?limit=${limit}` : '';
            return apiClient<Resource[]>(`/api/user/resources${queryParam}`);
        },
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useSharedResources() {
    return useQuery<Resource[]>({
        queryKey: resourceKeys.shared(),
        queryFn: () => apiClient<Resource[]>('/api/user/resources?shared=true'),
        staleTime: 2 * 60 * 1000,
    });
}

export function useResourceStatistics() {
    return useQuery<{ count: number }>({
        queryKey: resourceKeys.statistics(),
        queryFn: async () => {
            const { fetchWithAuth } = await import('@/_lib/fetch-with-auth');
            const response = await fetchWithAuth('/api/user/statistics/resources');
            if (!response.ok) throw new Error('Failed to fetch resource statistics');
            return response.json();
        },
        staleTime: 5 * 60 * 1000, // 5 minutes
    });
}

export function useDeleteResource() {
    const queryClient = useQueryClient();
    
    return useMutation({
        mutationFn: (resourceId: string) => 
            apiClient(`/api/user/resources/${resourceId}`, { method: 'DELETE' }),
        onMutate: async (resourceId) => {
            await queryClient.cancelQueries({ queryKey: resourceKeys.all });
            
            const previousResources = queryClient.getQueryData<Resource[]>(resourceKeys.lists());
            
            queryClient.setQueryData<Resource[]>(resourceKeys.lists(), (old) => {
                if (!old) return old;
                return old.filter((res) => res.id !== resourceId);
            });
            
            return { previousResources };
        },
        onError: (err, resourceId, context) => {
            if (context?.previousResources) {
                queryClient.setQueryData(resourceKeys.lists(), context.previousResources);
            }
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: resourceKeys.all });
        },
    });
}

