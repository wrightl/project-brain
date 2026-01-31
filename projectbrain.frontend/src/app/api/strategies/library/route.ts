import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createApiRoute(async (_req: NextRequest) => {
    const response = await callBackendApi('/strategies/library');
    if (!response.ok) {
        throw new Error('Failed to fetch strategies library');
    }
    return await response.json();
});

