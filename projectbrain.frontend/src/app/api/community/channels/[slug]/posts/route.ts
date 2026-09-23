import { createApiRoute } from '@/_lib/api-route-handler';
import {
    CommunityService,
    CommunityPost,
    CommunityPostsPage,
} from '@/_services/community-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<CommunityPostsPage>(
    async (
        req: NextRequest,
        { params }: { params: Promise<{ slug: string }> },
    ) => {
        const { slug } = await params;
        const { searchParams } = new URL(req.url);
        return CommunityService.getPosts(slug, {
            cursor: searchParams.get('cursor') ?? undefined,
            limit: searchParams.get('limit')
                ? parseInt(searchParams.get('limit')!, 10)
                : undefined,
        });
    },
);

export const POST = createApiRoute<CommunityPost>(
    async (
        req: NextRequest,
        { params }: { params: Promise<{ slug: string }> },
    ) => {
        const { slug } = await params;
        const body = await req.json();
        if (!body?.body || typeof body.body !== 'string') {
            throw new BackendApiError(400, 'Body is required');
        }
        return CommunityService.createPost(slug, body.body);
    },
);
