import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/_lib/api-client';
import { PagedResponse } from '@/_lib/types';
import {
    CoachRating,
    CreateCoachRatingRequest,
} from '@/_services/coach-rating-service';

export function useCoachRatings(
    coachId: string,
    options?: { page?: number; pageSize?: number }
) {
    return useQuery({
        queryKey: ['coach-ratings', coachId, options?.page, options?.pageSize],
        queryFn: async () => {
            const params = new URLSearchParams();
            if (options?.page) {
                params.append('page', options.page.toString());
            }
            if (options?.pageSize) {
                params.append('pageSize', options.pageSize.toString());
            }
            const queryParam = params.toString() ? `?${params.toString()}` : '';
            return apiClient<PagedResponse<CoachRating>>(
                `/api/coaches/${coachId}/ratings${queryParam}`
            );
        },
        enabled: !!coachId,
    });
}

export function useMyPersonalCoachRatings(options?: {
    page?: number;
    pageSize?: number;
}) {
    return useQuery({
        queryKey: ['coach-ratings', options?.page, options?.pageSize],
        queryFn: async () => {
            const params = new URLSearchParams();
            if (options?.page) {
                params.append('page', options.page.toString());
            }
            if (options?.pageSize) {
                params.append('pageSize', options.pageSize.toString());
            }
            const queryParam = params.toString() ? `?${params.toString()}` : '';
            return apiClient<PagedResponse<CoachRating>>(
                `/api/coaches/ratings/mine${queryParam}`
            );
        },
    });
}

export function useMyCoachRating(coachId: string) {
    return useQuery({
        queryKey: ['my-coach-rating', coachId],
        queryFn: () => {
            return apiClient<CoachRating | null>(
                `/api/coaches/${coachId}/ratings/mine`
            );
        },
        enabled: !!coachId,
    });
}

export function useCreateOrUpdateCoachRating(coachId: string) {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (request: CreateCoachRatingRequest) => {
            return apiClient<CoachRating>(`/api/coaches/${coachId}/ratings`, {
                method: 'POST',
                body: request,
            });
        },
        onSuccess: () => {
            // Invalidate related queries
            queryClient.invalidateQueries({
                queryKey: ['coach-ratings', coachId],
            });
            queryClient.invalidateQueries({
                queryKey: ['my-coach-rating', coachId],
            });
            queryClient.invalidateQueries({
                queryKey: ['coaches', coachId],
            });
            queryClient.invalidateQueries({
                queryKey: ['coaches', 'search'],
            });
        },
    });
}
