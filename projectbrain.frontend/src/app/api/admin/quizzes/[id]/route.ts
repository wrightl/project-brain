import { createAdminApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const GET = createAdminApiRoute(
    async (_req: NextRequest, context?: { params: Promise<{ id: string }> }) => {
        const { id } = await context!.params;
        const response = await callBackendApi(`/quizes/${id}`);
        if (!response.ok) {
            return Response.json(
                { error: 'Failed to fetch quiz' },
                { status: response.status }
            );
        }
        return await response.json();
    }
);

export const PUT = createAdminApiRoute(
    async (request: NextRequest, context?: { params: Promise<{ id: string }> }) => {
        const { id } = await context!.params;
        const body = await request.json();
        const response = await callBackendApi(`/quizes/${id}`, {
            method: 'PUT',
            body,
        });
        if (!response.ok) {
            const errorData = await response.json();
            return Response.json(errorData, { status: response.status });
        }
        return await response.json();
    }
);

export const DELETE = createAdminApiRoute(
    async (_req: NextRequest, context?: { params: Promise<{ id: string }> }) => {
        const { id } = await context!.params;
        const response = await callBackendApi(`/quizes/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            const errorData = await response.json();
            return Response.json(errorData, { status: response.status });
        }
        return { success: true };
    }
);
