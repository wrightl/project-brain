import { createAdminApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createAdminApiRoute(async (req: NextRequest) => {
    const response = await callBackendApi('/admin/settings/chat-memory');
    if (!response.ok) {
        throw new Error('Failed to fetch chat memory settings');
    }
    return response.json();
});

export const PUT = createAdminApiRoute(async (req: NextRequest) => {
    const body = await req.json();
    const response = await callBackendApi('/admin/settings/chat-memory', {
        method: 'PUT',
        body: body,
    });
    if (!response.ok) {
        throw new Error('Failed to update chat memory settings');
    }
    return response.json();
});
