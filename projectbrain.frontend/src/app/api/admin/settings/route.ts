import { createAdminApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createAdminApiRoute(async (req: NextRequest) => {
    const response = await callBackendApi('/admin/settings');
    if (!response.ok) {
        throw new Error('Failed to fetch settings');
    }
    return response.json();
});
