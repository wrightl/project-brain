import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createApiRoute(async (_req: NextRequest) => {
    const response = await callBackendApi('/achievements');
    if (!response.ok) {
        throw new Error('Failed to fetch achievements');
    }
    return await response.json();
});
