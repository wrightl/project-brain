import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi, BackendApiError } from '@/_lib/backend-api';

type RateStrategyRequest = {
    rating: number;
};

export const DELETE = createApiRoute(
    async (_req: NextRequest, { params }: { params: Promise<{ id: string }> }) => {
        const { id } = await params;

        const response = await callBackendApi(`/strategies/${id}`, {
            method: 'DELETE',
        });

        if (!response.ok) {
            throw new Error('Failed to delete strategy');
        }

        return { success: true };
    }
);

export const PUT = createApiRoute(
    async (req: NextRequest, { params }: { params: Promise<{ id: string }> }) => {
        const { id } = await params;
        const body = (await req.json()) as Partial<RateStrategyRequest>;

        const rating = body.rating;
        if (!Number.isInteger(rating) || rating < 1 || rating > 5) {
            throw new BackendApiError(400, 'Rating must be an integer between 1 and 5');
        }

        const response = await callBackendApi(`/strategies/${id}/rating`, {
            method: 'PUT',
            body: { rating },
        });

        if (!response.ok) {
            throw new Error('Failed to update rating');
        }

        // backend will likely return updated strategy item; if not, still ok
        return await response.json().catch(() => ({ success: true }));
    }
);

