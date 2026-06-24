import { createAdminApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createAdminApiRoute(async () => {
    const response = await callBackendApi('/admin/settings/memory-formation');
    if (!response.ok) {
        throw new Error('Failed to fetch memory formation settings');
    }
    return response.json();
});

export const PUT = createAdminApiRoute(async (req: NextRequest) => {
    const body = await req.json();
    const response = await callBackendApi('/admin/settings/memory-formation', {
        method: 'PUT',
        body: body,
    });
    if (!response.ok) {
        throw new Error('Failed to update memory formation settings');
    }
    return response.json();
});
