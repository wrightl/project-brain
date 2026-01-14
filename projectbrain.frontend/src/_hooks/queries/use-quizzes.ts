import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/_lib/api-client';
import { Quiz, QuizResponse } from '@/_services/quiz-service';
import { PagedResponse } from '@/_lib/types';

export const quizKeys = {
    all: ['quizzes'] as const,
    lists: () => [...quizKeys.all, 'list'] as const,
    list: (page?: number, pageSize?: number) =>
        [...quizKeys.lists(), page, pageSize] as const,
    details: () => [...quizKeys.all, 'detail'] as const,
    detail: (id: string) => [...quizKeys.details(), id] as const,
    responses: () => [...quizKeys.all, 'responses'] as const,
    responsesList: (page?: number, pageSize?: number) =>
        [...quizKeys.responses(), 'list', page, pageSize] as const,
    responsesCount: () => [...quizKeys.responses(), 'count'] as const,
    insights: () => [...quizKeys.all, 'insights'] as const,
};

export function useQuizzes(options?: { page?: number; pageSize?: number }) {
    return useQuery<PagedResponse<Quiz>>({
        queryKey: quizKeys.list(options?.page, options?.pageSize),
        queryFn: () => {
            const params = new URLSearchParams();
            if (options?.page) {
                params.append('page', options.page.toString());
            }
            if (options?.pageSize) {
                params.append('pageSize', options.pageSize.toString());
            }
            const queryParam = params.toString() ? `?${params.toString()}` : '';
            return apiClient<PagedResponse<Quiz>>(
                `/api/user/quizzes${queryParam}`
            );
        },
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useQuiz(quizId: string) {
    return useQuery<Quiz>({
        queryKey: quizKeys.detail(quizId),
        queryFn: () => apiClient<Quiz>(`/api/user/quizzes/${quizId}`),
        enabled: !!quizId,
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useQuizResponse(responseId: string) {
    return useQuery<QuizResponse>({
        queryKey: [...quizKeys.responses(), 'detail', responseId],
        queryFn: () =>
            apiClient<QuizResponse>(
                `/api/user/quizzes/responses/${responseId}`
            ),
        enabled: !!responseId,
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useQuizResponses(options?: {
    page?: number;
    pageSize?: number;
}) {
    return useQuery<PagedResponse<QuizResponse>>({
        queryKey: quizKeys.responsesList(options?.page, options?.pageSize),
        queryFn: () => {
            const params = new URLSearchParams();
            if (options?.page) {
                params.append('page', options.page.toString());
            }
            if (options?.pageSize) {
                params.append('pageSize', options.pageSize.toString());
            }
            const queryParam = params.toString() ? `?${params.toString()}` : '';
            return apiClient<PagedResponse<QuizResponse>>(
                `/api/user/quizzes/responses${queryParam}`
            );
        },
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useQuizResponseCount() {
    return useQuery<{ count: number }>({
        queryKey: quizKeys.responsesCount(),
        queryFn: () =>
            apiClient<{ count: number }>('/api/user/quizzes/responses/count'),
        staleTime: 5 * 60 * 1000, // 5 minutes
    });
}

export function useRecentQuizResponses(count: number = 3) {
    return useQuery<QuizResponse[]>({
        queryKey: [...quizKeys.responses(), 'recent', count],
        queryFn: async () => {
            const response = await apiClient<PagedResponse<QuizResponse>>(
                `/api/user/quizzes/responses?pageSize=${count}`
            );
            return response.items;
        },
        staleTime: 2 * 60 * 1000, // 2 minutes
    });
}

export function useQuizInsights() {
    return useQuery<{
        summary: string;
        keyInsights: string[];
        lastUpdated: string;
    }>({
        queryKey: quizKeys.insights(),
        queryFn: () =>
            apiClient<{
                summary: string;
                keyInsights: string[];
                lastUpdated: string;
            }>('/api/user/quizzes/insights'),
        staleTime: 5 * 60 * 1000, // 5 minutes
    });
}

export function useSubmitQuizResponse() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({
            quizId,
            answers,
            completedAt,
        }: {
            quizId: string;
            answers: Record<string, unknown>;
            completedAt?: string;
        }) => {
            const { fetchWithAuth } = require('@/_lib/fetch-with-auth');
            return fetchWithAuth(`/api/user/quizzes/${quizId}/responses`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ answers, completedAt }),
            }).then(async (response: Response) => {
                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(
                        errorText || 'Failed to submit quiz response'
                    );
                }
                return response.json() as Promise<QuizResponse>;
            });
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: quizKeys.responses() });
            queryClient.invalidateQueries({ queryKey: quizKeys.insights() });
            queryClient.invalidateQueries({
                queryKey: quizKeys.responsesCount(),
            });
        },
    });
}

export function useDeleteQuizResponse() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (responseId: string) => {
            return apiClient(`/api/user/quizzes/responses/${responseId}`, {
                method: 'DELETE',
            });
        },
        onMutate: async (responseId) => {
            await queryClient.cancelQueries({ queryKey: quizKeys.responses() });

            const previousData = queryClient.getQueriesData<
                PagedResponse<QuizResponse>
            >({
                queryKey: quizKeys.responsesList(),
            });

            previousData.forEach(([queryKey, data]) => {
                if (data) {
                    queryClient.setQueryData<PagedResponse<QuizResponse>>(
                        queryKey,
                        (old) => {
                            if (!old) return old;
                            return {
                                ...old,
                                items: old.items.filter(
                                    (response) => response.id !== responseId
                                ),
                                totalCount: old.totalCount - 1,
                            };
                        }
                    );
                }
            });

            return { previousData };
        },
        onError: (err, responseId, context) => {
            if (context?.previousData) {
                context.previousData.forEach(([queryKey, data]) => {
                    if (data) {
                        queryClient.setQueryData(queryKey, data);
                    }
                });
            }
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: quizKeys.responses() });
            queryClient.invalidateQueries({
                queryKey: quizKeys.responsesCount(),
            });
            queryClient.invalidateQueries({ queryKey: quizKeys.insights() });
        },
    });
}
