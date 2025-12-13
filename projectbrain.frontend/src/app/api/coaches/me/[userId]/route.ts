import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { Coach } from '@/_lib/types';
import { callBackendApi } from '@/_lib/backend-api';
import { BackendApiError } from '@/_lib/backend-api';

export const PUT = createApiRoute<Coach>(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    const userId = pathname.split('/').pop();

    if (!userId) {
        throw new BackendApiError(400, 'User ID is required');
    }

    const body = await req.json();
    const response = await callBackendApi(`/coaches/me/${userId}`, {
        method: 'PUT',
        body: body,
    });

    if (!response.ok) {
        const errorData = await response.json();
        throw new BackendApiError(
            response.status,
            errorData.message || 'Failed to update coach profile'
        );
    }

    return await response.json();
});
