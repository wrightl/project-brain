import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Connection } from '@/_lib/types';
import { apiClient } from '@/_lib/api-client';
import { ConversationSummary } from '@/_services/coach-message-service';

export const connectionKeys = {
    all: ['connections'] as const,
    lists: () => [...connectionKeys.all, 'list'] as const,
    list: () => [...connectionKeys.lists()] as const,
    details: () => [...connectionKeys.all, 'detail'] as const,
    detail: (id: string) => [...connectionKeys.details(), id] as const,
    conversations: () => [...connectionKeys.all, 'conversations'] as const,
};

export function useConnections() {
    return useQuery<Connection[]>({
        queryKey: connectionKeys.list(),
        queryFn: () => apiClient<Connection[]>('/api/connections'),
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useConnection(connectionId: string) {
    return useQuery<Connection | null>({
        queryKey: connectionKeys.detail(connectionId),
        queryFn: async () => {
            try {
                return await apiClient<Connection>(`/api/connections/${connectionId}`);
            } catch (error: any) {
                if (error?.status === 404) {
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
            const previousConnections = queryClient.getQueryData<Connection[]>(connectionKeys.list());
            
            // Optimistically update to remove the connection
            queryClient.setQueryData<Connection[]>(connectionKeys.list(), (old) => {
                if (!old) return old;
                return old.filter((conn) => conn.id !== connectionId);
            });
            
            // Return context with the previous value
            return { previousConnections };
        },
        onError: (err, connectionId, context) => {
            // If the mutation fails, use the context returned from onMutate to roll back
            if (context?.previousConnections) {
                queryClient.setQueryData(connectionKeys.list(), context.previousConnections);
            }
        },
        onSettled: () => {
            // Always refetch after error or success to ensure consistency
            queryClient.invalidateQueries({ queryKey: connectionKeys.all });
        },
    });
}

