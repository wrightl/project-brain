import { createApiRoute } from '@/_lib/api-route-handler';
import {
    CommunityService,
    CommunityPostsPage,
} from '@/_services/community-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<CommunityPostsPage>(
    async (req: NextRequest) => {
        const { searchParams } = new URL(req.url);
        return CommunityService.getLatestFeed({
            cursor: searchParams.get('cursor') ?? undefined,
            limit: searchParams.get('limit')
                ? parseInt(searchParams.get('limit')!, 10)
                : undefined,
        });
    },
);
