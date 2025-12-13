import { createApiRoute } from '@/_lib/api-route-handler';
import { Resource } from '@/_lib/types';
import { ResourceService } from '@/_services/resource-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<Resource[]>(async (req: NextRequest) => {
    const { searchParams } = new URL(req.url);
    const limitParam = searchParams.get('limit');
    const limit = limitParam ? parseInt(limitParam, 10) : undefined;

    return await ResourceService.getResources(limit);
});
