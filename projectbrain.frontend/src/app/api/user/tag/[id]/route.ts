import { createApiRoute } from '@/_lib/api-route-handler';
import { TagService, Tag } from '@/_services/tag-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<Tag>(
    async (req: NextRequest, { params }: { params: { id: string } }) => {
        const tag = await TagService.getTag(params.id);
        return tag;
    }
);

export const PUT = createApiRoute<Tag>(
    async (req: NextRequest, { params }: { params: { id: string } }) => {
        const body = await req.json();
        const { name } = body;

        if (!name || typeof name !== 'string') {
            throw new BackendApiError(400, 'Name is required');
        }

        const tag = await TagService.updateTag(params.id, { name });
        return tag;
    }
);

export const DELETE = createApiRoute<void>(
    async (req: NextRequest, { params }: { params: { id: string } }) => {
        await TagService.deleteTag(params.id);
    }
);

