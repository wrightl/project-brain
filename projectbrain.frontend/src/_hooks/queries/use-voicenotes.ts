import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { VoiceNote, PagedResponse } from '@/_lib/types';
import { apiClient } from '@/_lib/api-client';

export const voicenoteKeys = {
    all: ['voicenotes'] as const,
    lists: () => [...voicenoteKeys.all, 'list'] as const,
    list: (page?: number, pageSize?: number) => [...voicenoteKeys.lists(), page, pageSize] as const,
    details: () => [...voicenoteKeys.all, 'detail'] as const,
    detail: (id: string) => [...voicenoteKeys.details(), id] as const,
    statistics: () => [...voicenoteKeys.all, 'statistics'] as const,
};

export function useVoiceNotes(options?: {
    page?: number;
    pageSize?: number;
}) {
    return useQuery<PagedResponse<VoiceNote>>({
        queryKey: voicenoteKeys.list(options?.page, options?.pageSize),
        queryFn: () => {
            const params = new URLSearchParams();
            if (options?.page) {
                params.append('page', options.page.toString());
            }
            if (options?.pageSize) {
                params.append('pageSize', options.pageSize.toString());
            }
            const queryParam = params.toString() ? `?${params.toString()}` : '';
            return apiClient<PagedResponse<VoiceNote>>(`/api/user/voicenotes${queryParam}`);
        },
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useVoiceNoteStatistics() {
    return useQuery<{ count: number }>({
        queryKey: voicenoteKeys.statistics(),
        queryFn: async () => {
            const { fetchWithAuth } = await import('@/_lib/fetch-with-auth');
            const response = await fetchWithAuth('/api/user/statistics/voicenotes');
            if (!response.ok) throw new Error('Failed to fetch voice note statistics');
            return response.json();
        },
        staleTime: 5 * 60 * 1000, // 5 minutes
    });
}

export function useDeleteVoiceNote() {
    const queryClient = useQueryClient();
    
    return useMutation({
        mutationFn: (voiceNoteId: string) => 
            apiClient(`/api/user/voicenotes/${voiceNoteId}`, { method: 'DELETE' }),
        onMutate: async (voiceNoteId) => {
            await queryClient.cancelQueries({ queryKey: voicenoteKeys.all });
            
            const previousData = queryClient.getQueriesData<PagedResponse<VoiceNote>>({ queryKey: voicenoteKeys.lists() });
            
            // Update all list queries to remove the deleted voice note
            previousData.forEach(([queryKey, data]) => {
                if (data) {
                    queryClient.setQueryData<PagedResponse<VoiceNote>>(queryKey, (old) => {
                        if (!old) return old;
                        return {
                            ...old,
                            items: old.items.filter((vn) => vn.id !== voiceNoteId),
                            totalCount: old.totalCount - 1,
                        };
                    });
                }
            });
            
            return { previousData };
        },
        onError: (err, voiceNoteId, context) => {
            // Restore previous data on error
            if (context?.previousData) {
                context.previousData.forEach(([queryKey, data]) => {
                    if (data) {
                        queryClient.setQueryData(queryKey, data);
                    }
                });
            }
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: voicenoteKeys.all });
        },
    });
}

export function useUploadVoiceNote() {
    const queryClient = useQueryClient();
    
    return useMutation({
        mutationFn: async ({ file, description }: { file: File; description?: string }) => {
            const formData = new FormData();
            formData.append('file', file);
            if (description && description.trim()) {
                formData.append('description', description.trim());
            }
            
            const { fetchWithAuth } = await import('@/_lib/fetch-with-auth');
            const response = await fetchWithAuth('/api/user/voicenotes', {
                method: 'POST',
                body: formData,
            });
            
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || 'Failed to upload voice note');
            }
            
            return response.json() as Promise<VoiceNote>;
        },
        onSuccess: (newVoiceNote) => {
            // Optimistically add the new voice note to all list queries
            queryClient.setQueriesData<PagedResponse<VoiceNote>>({ queryKey: voicenoteKeys.lists() }, (old) => {
                if (!old) {
                    return {
                        items: [newVoiceNote],
                        page: 1,
                        pageSize: 20,
                        totalCount: 1,
                        totalPages: 1,
                        hasPreviousPage: false,
                        hasNextPage: false,
                    };
                }
                return {
                    ...old,
                    items: [newVoiceNote, ...old.items],
                    totalCount: old.totalCount + 1,
                };
            });
            queryClient.invalidateQueries({ queryKey: voicenoteKeys.all });
        },
    });
}

