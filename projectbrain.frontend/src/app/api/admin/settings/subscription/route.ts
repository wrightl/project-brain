import { createApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createApiRoute(async (req: NextRequest) => {
    const response = await callBackendApi('/admin/settings/subscription');
    if (!response.ok) {
        throw new Error('Failed to fetch subscription settings');
    }
    return response.json();
});

export const PUT = createApiRoute(async (req: NextRequest) => {
    const body = await req.json();
    const response = await callBackendApi('/admin/settings/subscription', {
        method: 'PUT',
        body: body,
    });
    if (!response.ok) {
        throw new Error('Failed to update subscription settings');
    }
    return response.json();
});

