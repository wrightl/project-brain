import { createApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';
import { callBackendApi } from '@/_lib/backend-api';

export const POST = createApiRoute(async (
    _req: NextRequest,
    { params }: { params: Promise<{ id: string }> }
) => {
    const { id } = await params;
    const response = await callBackendApi(`/user/memory/facts/${id}/unpin`, {
        method: 'POST',
    });
    if (!response.ok) {
        throw new Error('Failed to unpin memory');
    }
    return { success: true };
});
