import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { User } from '@/_lib/types';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<User>(async (req: NextRequest) => {
    const body = await req.json();

    const response = await callBackendApi('/users/me', {
        scopes: ['read:users'],
        method: 'GET',
        body: body,
    });

    return await response.json();
});
