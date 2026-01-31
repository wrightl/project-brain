import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createApiRoute(async (_req: NextRequest) => {
    const response = await callBackendApi('/coping-strategies/library');
    if (!response.ok) {
        throw new Error('Failed to fetch coping strategy library');
    }
    return await response.json();
});
