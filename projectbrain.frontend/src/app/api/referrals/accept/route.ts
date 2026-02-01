import { createApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';
import { callBackendApi } from '@/_lib/backend-api';

export const POST = createApiRoute(async (req: NextRequest) => {
    const body = await req.json();
    const response = await callBackendApi('/referrals/accept', {
        method: 'POST',
        body,
    });
    if (!response.ok) {
        throw new Error('Failed to accept referral invite');
    }
    return response.json();
});

