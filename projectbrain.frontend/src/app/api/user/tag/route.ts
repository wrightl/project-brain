import { createApiRoute } from '@/_lib/api-route-handler';
import { TagService, Tag } from '@/_services/tag-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<Tag[]>(async () => {
    const result = await TagService.getAllTags();
    return result;
});

export const POST = createApiRoute<Tag>(async (req: NextRequest) => {
    const body = await req.json();
    const { name } = body;

    if (!name || typeof name !== 'string') {
        throw new BackendApiError(400, 'Name is required');
    }

    const tag = await TagService.createTag({ name });
    return tag;
});

