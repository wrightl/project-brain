import { createApiRoute } from '@/_lib/api-route-handler';
import { ReindexResult } from '@/_lib/types';
import { ResourceService } from '@/_services/resource-service';
import { NextRequest, NextResponse } from 'next/server';

export const POST = createApiRoute<ReindexResult>(async (req: NextRequest) => {
    const result = await ResourceService.reindexResources();
    return result;
});
