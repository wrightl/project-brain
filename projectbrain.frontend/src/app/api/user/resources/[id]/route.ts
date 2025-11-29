import { NextRequest, NextResponse } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { ResourceService } from '@/_services/resource-service';
import { Resource } from '@/_lib/types';
import { BackendApiError } from '@/_lib/backend-api';

export const GET = createApiRoute<Resource>(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    const id = pathname.split('/').pop();

    if (!id) {
        throw new BackendApiError(400, 'Resource ID is required');
    }

    const resource = await ResourceService.getResource(id);

    if (!resource) {
        throw new BackendApiError(404, 'Resource not found');
    }

    return resource;
});

export const DELETE = createApiRoute(async (req: NextRequest) => {
    try {
        // Extract the ID from the URL pathname
        // URL format: /api/user/resources/[id]
        const pathname = req.nextUrl.pathname;
        const id = pathname.split('/').pop();

        if (!id) {
            return NextResponse.json(
                { error: 'Resource ID is required' },
                { status: 400 }
            );
        }

        const response = await ResourceService.deleteResource(id);

        if (response.ok) {
            return NextResponse.json(
                { error: 'Failed to delete resource' },
                { status: response.status }
            );
        }

        return NextResponse.json({ success: true });
    } catch (error) {
        console.error('Delete resource error:', error);
        return NextResponse.json(
            { error: 'Internal server error' },
            { status: 500 }
        );
    }
});
