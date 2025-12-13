import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { VoiceNote } from '@/_lib/types';
import { apiClient } from '@/_lib/api-client';

export const voicenoteKeys = {
    all: ['voicenotes'] as const,
    lists: () => [...voicenoteKeys.all, 'list'] as const,
    list: (limit?: number) => [...voicenoteKeys.lists(), limit] as const,
    details: () => [...voicenoteKeys.all, 'detail'] as const,
    detail: (id: string) => [...voicenoteKeys.details(), id] as const,
    statistics: () => [...voicenoteKeys.all, 'statistics'] as const,
};

export function useVoiceNotes(limit?: number) {
    return useQuery<VoiceNote[]>({
        queryKey: voicenoteKeys.list(limit),
        queryFn: () => {
            const queryParam = limit ? `?limit=${limit}` : '';
            return apiClient<VoiceNote[]>(`/api/user/voicenotes${queryParam}`);
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
            
            const previousVoiceNotes = queryClient.getQueryData<VoiceNote[]>(voicenoteKeys.lists());
            
            queryClient.setQueryData<VoiceNote[]>(voicenoteKeys.lists(), (old) => {
                if (!old) return old;
                return old.filter((vn) => vn.id !== voiceNoteId);
            });
            
            return { previousVoiceNotes };
        },
        onError: (err, voiceNoteId, context) => {
            if (context?.previousVoiceNotes) {
                queryClient.setQueryData(voicenoteKeys.lists(), context.previousVoiceNotes);
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
            // Optimistically add the new voice note to the list
            queryClient.setQueryData<VoiceNote[]>(voicenoteKeys.lists(), (old) => {
                if (!old) return [newVoiceNote];
                return [newVoiceNote, ...old];
            });
            queryClient.invalidateQueries({ queryKey: voicenoteKeys.all });
        },
    });
}

