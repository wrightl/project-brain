import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Resource, PagedResponse } from '@/_lib/types';
import { apiClient } from '@/_lib/api-client';

export const resourceKeys = {
    all: ['resources'] as const,
    lists: () => [...resourceKeys.all, 'list'] as const,
    list: (page?: number, pageSize?: number) => [...resourceKeys.lists(), page, pageSize] as const,
    details: () => [...resourceKeys.all, 'detail'] as const,
    detail: (id: string) => [...resourceKeys.details(), id] as const,
    shared: () => [...resourceKeys.all, 'shared'] as const,
    statistics: () => [...resourceKeys.all, 'statistics'] as const,
};

export function useResources(options?: {
    page?: number;
    pageSize?: number;
}) {
    return useQuery<PagedResponse<Resource>>({
        queryKey: resourceKeys.list(options?.page, options?.pageSize),
        queryFn: () => {
            const params = new URLSearchParams();
            if (options?.page) {
                params.append('page', options.page.toString());
            }
            if (options?.pageSize) {
                params.append('pageSize', options.pageSize.toString());
            }
            const queryParam = params.toString() ? `?${params.toString()}` : '';
            return apiClient<PagedResponse<Resource>>(`/api/user/resources${queryParam}`);
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
            
            const previousResources = queryClient.getQueriesData<PagedResponse<Resource>>({ queryKey: resourceKeys.lists() });
            
            // Update all list queries to remove the deleted resource
            previousResources.forEach(([queryKey, data]) => {
                if (data) {
                    queryClient.setQueryData<PagedResponse<Resource>>(queryKey, (old) => {
                        if (!old) return old;
                        return {
                            ...old,
                            items: old.items.filter((res) => res.id !== resourceId),
                            totalCount: old.totalCount - 1,
                        };
                    });
                }
            });
            
            return { previousResources };
        },
        onError: (err, resourceId, context) => {
            if (context?.previousResources) {
                context.previousResources.forEach(([queryKey, data]) => {
                    if (data) {
                        queryClient.setQueryData(queryKey, data);
                    }
                });
            }
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: resourceKeys.all });
        },
    });
}

