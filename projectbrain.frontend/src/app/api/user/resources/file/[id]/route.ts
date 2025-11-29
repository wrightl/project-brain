import { NextRequest, NextResponse } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { BackendApiError } from '@/_lib/backend-api';
import { ResourceService } from '@/_services/resource-service';

export const GET = createApiRoute(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    const id = pathname.split('/').pop();

    if (!id) {
        throw new BackendApiError(400, 'Resource ID is required');
    }

    const { blob, headers } = await ResourceService.getUserResourceFile(id);

    if (!blob) {
        throw new BackendApiError(404, 'Resource not found');
    }

    // Return the blob as a NextResponse with proper headers
    return new NextResponse(blob, {
        status: 200,
        headers,
    });
});
