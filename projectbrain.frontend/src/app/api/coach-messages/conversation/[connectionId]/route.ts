import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const GET = createApiRoute(
    async (req: NextRequest, context?: { params: Promise<{ connectionId: string }> }) => {
        const { connectionId } = await context!.params;
        const searchParams = req.nextUrl.searchParams;
        const pageSize = searchParams.get('pageSize') || '20';
        const beforeDate = searchParams.get('beforeDate');

        const queryParams = new URLSearchParams();
        queryParams.append('pageSize', pageSize);
        if (beforeDate) {
            queryParams.append('beforeDate', beforeDate);
        }

        const response = await callBackendApi(
            `/coach-messages/conversation/${connectionId}?${queryParams.toString()}`
        );

        if (!response.ok) {
            return Response.json(
                { error: 'Failed to fetch conversation messages' },
                { status: response.status }
            );
        }

        return await response.json();
    }
);

export const PUT = createApiRoute(
    async (_req, context?: { params: Promise<{ connectionId: string }> }) => {
        const { connectionId } = await context!.params;

        const response = await callBackendApi(
            `/coach-messages/conversation/${connectionId}/read`,
            { method: 'PUT' }
        );

        if (!response.ok) {
            return Response.json(
                { error: 'Failed to mark conversation as read' },
                { status: response.status }
            );
        }

        return { success: true };
    }
);
