import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/_lib/api-client';
import {
    JournalEntry,
    PagedResponse,
    CreateJournalEntryRequest,
    UpdateJournalEntryRequest,
} from '@/_services/journal-service';

export const journalKeys = {
    all: ['journals'] as const,
    lists: () => [...journalKeys.all, 'list'] as const,
    list: (page?: number, pageSize?: number) =>
        [...journalKeys.lists(), page, pageSize] as const,
    details: () => [...journalKeys.all, 'detail'] as const,
    detail: (id: string) => [...journalKeys.details(), id] as const,
    count: () => [...journalKeys.all, 'count'] as const,
    recent: (count?: number) => [...journalKeys.all, 'recent', count] as const,
};

export function useJournalEntries(options?: {
    page?: number;
    pageSize?: number;
}) {
    return useQuery<PagedResponse<JournalEntry>>({
        queryKey: journalKeys.list(options?.page, options?.pageSize),
        queryFn: () => {
            const params = new URLSearchParams();
            if (options?.page) {
                params.append('page', options.page.toString());
            }
            if (options?.pageSize) {
                params.append('pageSize', options.pageSize.toString());
            }
            const queryParam = params.toString() ? `?${params.toString()}` : '';
            return apiClient<PagedResponse<JournalEntry>>(
                `/api/user/journal${queryParam}`
            );
        },
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useJournalEntry(id: string) {
    return useQuery<JournalEntry>({
        queryKey: journalKeys.detail(id),
        queryFn: () => apiClient<JournalEntry>(`/api/user/journal/${id}`),
        enabled: !!id,
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useJournalEntryCount() {
    return useQuery<{ count: number }>({
        queryKey: journalKeys.count(),
        queryFn: () => apiClient<{ count: number }>('/api/user/journal/count'),
        staleTime: 5 * 60 * 1000, // 5 minutes
    });
}

export function useRecentJournalEntries(count: number = 3) {
    return useQuery<JournalEntry[]>({
        queryKey: journalKeys.recent(count),
        queryFn: () =>
            apiClient<JournalEntry[]>(
                `/api/user/journal/recent?count=${count}`
            ),
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useCreateJournalEntry() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (request: CreateJournalEntryRequest) => {
            const { fetchWithAuth } = require('@/_lib/fetch-with-auth');
            return fetchWithAuth('/api/user/journal', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(request),
            }).then(async (response: Response) => {
                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(
                        errorText || 'Failed to create journal entry'
                    );
                }
                return response.json() as Promise<JournalEntry>;
            });
        },
        onSuccess: (newEntry) => {
            queryClient.invalidateQueries({ queryKey: journalKeys.all });
        },
    });
}

export function useUpdateJournalEntry() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({
            id,
            request,
        }: {
            id: string;
            request: UpdateJournalEntryRequest;
        }) => {
            const { fetchWithAuth } = require('@/_lib/fetch-with-auth');
            return fetchWithAuth(`/api/user/journal/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(request),
            }).then(async (response: Response) => {
                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(
                        errorText || 'Failed to update journal entry'
                    );
                }
                return response.json() as Promise<JournalEntry>;
            });
        },
        onSuccess: (updatedEntry) => {
            queryClient.invalidateQueries({ queryKey: journalKeys.all });
            queryClient.invalidateQueries({
                queryKey: journalKeys.detail(updatedEntry.id),
            });
        },
    });
}

export function useDeleteJournalEntry() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (journalEntryId: string) => {
            return apiClient(`/api/user/journal/${journalEntryId}`, {
                method: 'DELETE',
            });
        },
        onMutate: async (journalEntryId) => {
            await queryClient.cancelQueries({ queryKey: journalKeys.all });

            const previousData = queryClient.getQueriesData<
                PagedResponse<JournalEntry>
            >({
                queryKey: journalKeys.lists(),
            });

            previousData.forEach(([queryKey, data]) => {
                if (data) {
                    queryClient.setQueryData<PagedResponse<JournalEntry>>(
                        queryKey,
                        (old) => {
                            if (!old) return old;
                            return {
                                ...old,
                                items: old.items.filter(
                                    (je) => je.id !== journalEntryId
                                ),
                                totalCount: old.totalCount - 1,
                            };
                        }
                    );
                }
            });

            return { previousData };
        },
        onError: (err, journalEntryId, context) => {
            if (context?.previousData) {
                context.previousData.forEach(([queryKey, data]) => {
                    if (data) {
                        queryClient.setQueryData(queryKey, data);
                    }
                });
            }
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: journalKeys.all });
        },
    });
}
