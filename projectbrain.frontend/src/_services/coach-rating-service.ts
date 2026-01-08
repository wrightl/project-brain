import { callBackendApi } from '@/_lib/backend-api';
import { PagedResponse } from '@/_lib/types';

export interface CoachRating {
    id: string;
    userId: string;
    coachId: string;
    userName: string;
    rating: number;
    feedback?: string;
    createdAt: string;
    updatedAt: string;
}

export interface CreateCoachRatingRequest {
    rating: number;
    feedback?: string;
}

export class CoachRatingService {
    /**
     * Create or update a rating for a coach
     */
    static async createOrUpdateRating(
        coachId: string,
        request: CreateCoachRatingRequest
    ): Promise<CoachRating> {
        const response = await callBackendApi(`/coaches/${coachId}/ratings`, {
            method: 'POST',
            body: request,
        });

        if (!response.ok) {
            throw new Error('Failed to create/update rating');
        }

        return response.json();
    }

    /**
     * Get ratings for a coach with pagination
     */
    static async getRatings(
        coachId: string,
        options?: {
            page?: number;
            pageSize?: number;
        }
    ): Promise<PagedResponse<CoachRating>> {
        const params = new URLSearchParams();
        if (options?.page) {
            params.append('page', options.page.toString());
        }
        if (options?.pageSize) {
            params.append('pageSize', options.pageSize.toString());
        }

        const queryString = params.toString();
        const endpoint = `/coaches/${coachId}/ratings${
            queryString ? `?${queryString}` : ''
        }`;

        const response = await callBackendApi(endpoint);
        if (!response.ok) {
            throw new Error('Failed to get ratings');
        }

        return response.json();
    }

    /**
     * Get the current user's ratings for a coach
     */
    static async getMyRatings(options?: {
        page?: number;
        pageSize?: number;
    }): Promise<PagedResponse<CoachRating>> {
        const params = new URLSearchParams();
        if (options?.page) {
            params.append('page', options.page.toString());
        }
        if (options?.pageSize) {
            params.append('pageSize', options.pageSize.toString());
        }

        const queryString = params.toString();
        const endpoint = `/coaches/ratings/mine${
            queryString ? `?${queryString}` : ''
        }`;

        const response = await callBackendApi(endpoint);
        if (!response.ok) {
            throw new Error('Failed to get ratings');
        }

        return response.json();
    }

    /**
     * Get the current user's rating for a coach
     */
    static async getMyRating(coachId: string): Promise<CoachRating | null> {
        const response = await callBackendApi(
            `/coaches/${coachId}/ratings/mine`
        );

        if (response.status === 404) {
            return null;
        }

        if (!response.ok) {
            throw new Error('Failed to get rating');
        }

        return response.json();
    }
}




