import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { Resource } from '@/_lib/types';

export const GET = createApiRoute<Resource[]>(async () => {
    const response = await callBackendApi('/resource', {
        method: 'GET',
    });

    if (!response.ok) {
        throw new Error('Failed to fetch resources');
    }

    const resources: Resource[] = await response.json();
    return resources;
});

