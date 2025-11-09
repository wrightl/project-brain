import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { User } from '@/_lib/types';

export const GET = createApiRoute<User>(async () => {
    const response = await callBackendApi('/users/me', {
        scopes: ['read:users'],
        method: 'GET',
    });

    return await response.json();
});
