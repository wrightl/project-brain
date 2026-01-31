import {
    useQuery,
    useMutation,
    useQueryClient,
    type UseMutationResult,
} from '@tanstack/react-query';
import { apiClient } from '@/_lib/api-client';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import {
    JournalEntry,
    CreateJournalEntryRequest,
    UpdateJournalEntryRequest,
    SystemTag,
} from '@/_services/journal-service';
import { PagedResponse } from '@/_lib/types';

export const journalKeys = {
    all: ['journals'] as const,
    lists: () => [...journalKeys.all, 'list'] as const,
    list: (page?: number, pageSize?: number) =>
        [...journalKeys.lists(), page, pageSize] as const,
    details: () => [...journalKeys.all, 'detail'] as const,
    detail: (id: string) => [...journalKeys.details(), id] as const,
    count: () => [...journalKeys.all, 'count'] as const,
    recent: (count?: number) => [...journalKeys.all, 'recent', count] as const,
    streakSummary: () => [...journalKeys.all, 'streakSummary'] as const,
    systemTags: () => [...journalKeys.all, 'systemTags'] as const,
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
                `/api/user/journal${queryParam}`,
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
                `/api/user/journal/recent?count=${count}`,
            ),
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useJournalStreakSummary() {
    return useQuery<{ currentStreak: number; longestStreak: number }>({
        queryKey: journalKeys.streakSummary(),
        queryFn: () =>
            apiClient<{ currentStreak: number; longestStreak: number }>(
                '/api/user/journal/streak-summary',
            ),
        staleTime: 5 * 60 * 1000, // 5 minutes
    });
}

export function useJournalSystemTags() {
    return useQuery<SystemTag[]>({
        queryKey: journalKeys.systemTags(),
        queryFn: () => apiClient<SystemTag[]>('/api/user/journal/system-tags'),
        staleTime: 30 * 60 * 1000, // 30 minutes
    });
}

export function useCreateJournalEntry(): UseMutationResult<
    JournalEntry,
    Error,
    CreateJournalEntryRequest,
    unknown
> {
    const queryClient = useQueryClient();

    return useMutation<JournalEntry, Error, CreateJournalEntryRequest>({
        mutationFn: (request: CreateJournalEntryRequest) => {
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
                        errorText || 'Failed to create journal entry',
                    );
                }
                return response.json() as Promise<JournalEntry>;
            });
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: journalKeys.all });
        },
    });
}

export function useUpdateJournalEntry(): UseMutationResult<
    JournalEntry,
    Error,
    { id: string; request: UpdateJournalEntryRequest },
    unknown
> {
    const queryClient = useQueryClient();

    return useMutation<
        JournalEntry,
        Error,
        { id: string; request: UpdateJournalEntryRequest }
    >({
        mutationFn: ({ id, request }) => {
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
                        errorText || 'Failed to update journal entry',
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

export function useDeleteJournalEntry(): UseMutationResult<
    void,
    Error,
    string,
    { previousData: Array<[unknown, PagedResponse<JournalEntry> | undefined]> }
> {
    const queryClient = useQueryClient();

    return useMutation<
        void,
        Error,
        string,
        {
            previousData: Array<
                [unknown, PagedResponse<JournalEntry> | undefined]
            >;
        }
    >({
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
                                    (je) => je.id !== journalEntryId,
                                ),
                                totalCount: old.totalCount - 1,
                            };
                        },
                    );
                }
            });

            return { previousData };
        },
        onError: (err, journalEntryId, context) => {
            if (context?.previousData) {
                context.previousData.forEach(
                    ([queryKey, data]: [
                        unknown,
                        PagedResponse<JournalEntry> | undefined,
                    ]) => {
                        if (data) {
                            queryClient.setQueryData<
                                PagedResponse<JournalEntry>
                            >(queryKey as readonly unknown[], data);
                        }
                    },
                );
            }
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: journalKeys.all });
        },
    });
}
