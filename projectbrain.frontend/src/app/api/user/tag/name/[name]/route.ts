import { createApiRoute } from '@/_lib/api-route-handler';
import { TagService, Tag } from '@/_services/tag-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<Tag | null>(
    async (req: NextRequest, { params }: { params: { name: string } }) => {
        const decodedName = decodeURIComponent(params.name);
        const tag = await TagService.getTagByName(decodedName);
        return tag;
    }
);

