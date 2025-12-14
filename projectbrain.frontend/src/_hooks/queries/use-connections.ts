import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Connection, PagedResponse } from '@/_lib/types';
import { apiClient, ApiClientError } from '@/_lib/api-client';
import { ConversationSummary } from '@/_services/coach-message-service';

export const connectionKeys = {
    all: ['connections'] as const,
    lists: () => [...connectionKeys.all, 'list'] as const,
    list: (page?: number, pageSize?: number) => [...connectionKeys.lists(), page, pageSize] as const,
    details: () => [...connectionKeys.all, 'detail'] as const,
    detail: (id: string) => [...connectionKeys.details(), id] as const,
    conversations: () => [...connectionKeys.all, 'conversations'] as const,
};

export function useConnections(options?: {
    page?: number;
    pageSize?: number;
}) {
    return useQuery<PagedResponse<Connection>>({
        queryKey: connectionKeys.list(options?.page, options?.pageSize),
        queryFn: () => {
            const params = new URLSearchParams();
            if (options?.page) {
                params.append('page', options.page.toString());
            }
            if (options?.pageSize) {
                params.append('pageSize', options.pageSize.toString());
            }
            const queryParam = params.toString() ? `?${params.toString()}` : '';
            return apiClient<PagedResponse<Connection>>(`/api/connections${queryParam}`);
        },
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useConnection(connectionId: string) {
    return useQuery<Connection | null>({
        queryKey: connectionKeys.detail(connectionId),
        queryFn: async () => {
            try {
                return await apiClient<Connection>(`/api/connections/${connectionId}`);
            } catch (error) {
                if (error instanceof ApiClientError && error.status === 404) {
                    return null;
                }
                throw error;
            }
        },
        enabled: !!connectionId,
        staleTime: 2 * 60 * 1000,
    });
}

export function useConversations() {
    return useQuery<ConversationSummary[]>({
        queryKey: connectionKeys.conversations(),
        queryFn: async () => {
            const { fetchWithAuth } = await import('@/_lib/fetch-with-auth');
            const response = await fetchWithAuth('/api/coach-messages/conversations');
            if (!response.ok) throw new Error('Failed to fetch conversations');
            return response.json();
        },
        staleTime: 1 * 60 * 1000, // 1 minute
    });
}

export function useDeleteConnection() {
    const queryClient = useQueryClient();
    
    return useMutation({
        mutationFn: (connectionId: string) => 
            apiClient(`/api/connections/${connectionId}`, { method: 'DELETE' }),
        onMutate: async (connectionId) => {
            // Cancel any outgoing refetches
            await queryClient.cancelQueries({ queryKey: connectionKeys.all });
            
            // Snapshot the previous value
            const previousConnections = queryClient.getQueriesData<PagedResponse<Connection>>({ queryKey: connectionKeys.lists() });
            
            // Optimistically update all list queries to remove the connection
            previousConnections.forEach(([queryKey, data]) => {
                if (data) {
                    queryClient.setQueryData<PagedResponse<Connection>>(queryKey, (old) => {
                        if (!old) return old;
                        return {
                            ...old,
                            items: old.items.filter((conn) => conn.id !== connectionId),
                            totalCount: old.totalCount - 1,
                        };
                    });
                }
            });
            
            // Return context with the previous value
            return { previousConnections };
        },
        onError: (err, connectionId, context) => {
            // If the mutation fails, use the context returned from onMutate to roll back
            if (context?.previousConnections) {
                context.previousConnections.forEach(([queryKey, data]) => {
                    if (data) {
                        queryClient.setQueryData(queryKey, data);
                    }
                });
            }
        },
        onSettled: () => {
            // Always refetch after error or success to ensure consistency
            queryClient.invalidateQueries({ queryKey: connectionKeys.all });
        },
    });
}

