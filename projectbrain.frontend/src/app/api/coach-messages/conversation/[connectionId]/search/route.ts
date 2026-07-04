import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest, NextResponse } from 'next/server';

export const GET = createApiRoute(
    async (
        req: NextRequest,
        context?: { params: Promise<{ connectionId: string }> },
    ) => {
        const { connectionId } = await context!.params;
        const searchTerm = req.nextUrl.searchParams.get('searchTerm');

        if (!searchTerm) {
            return NextResponse.json(
                { error: 'Search term is required' },
                { status: 400 },
            );
        }

        const response = await callBackendApi(
            `/coach-messages/conversation/${connectionId}/search?searchTerm=${encodeURIComponent(searchTerm)}`,
        );

        if (!response.ok) {
            throw new Error('Failed to search messages');
        }

        return response.json();
    },
);
