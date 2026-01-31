import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi, BackendApiError } from '@/_lib/backend-api';

type CreateStrategyRequest = {
    title: string;
    description: string;
    iconKey?: string | null;
};

export const POST = createApiRoute(async (req: NextRequest) => {
    const body = (await req.json()) as Partial<CreateStrategyRequest>;

    if (!body.title || !body.description) {
        throw new BackendApiError(400, 'Title and description are required');
    }

    const response = await callBackendApi('/strategies', {
        method: 'POST',
        body: {
            title: body.title,
            description: body.description,
            iconKey: body.iconKey ?? null,
        },
    });

    if (!response.ok) {
        throw new Error('Failed to create strategy');
    }

    return await response.json();
});

