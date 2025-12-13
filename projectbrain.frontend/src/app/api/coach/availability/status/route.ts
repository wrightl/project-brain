import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachService } from '@/_services/coach-service';
import { NextRequest } from 'next/server';

type AvailabilityStatus = 'Available' | 'Busy' | 'Away' | 'Offline';

export const GET = createApiRoute<{ status: AvailabilityStatus }>(
    async (req: NextRequest) => {
        const status = await CoachService.getAvailabilityStatus();
        return { status };
    }
);

export const PUT = createApiRoute(async (req: NextRequest) => {
    const body = await req.json();
    const { status } = body as { status: AvailabilityStatus };

    if (!status) {
        return new Response(
            JSON.stringify({ error: 'Status is required' }),
            { status: 400 }
        );
    }

    await CoachService.setAvailabilityStatus(status);
    return { success: true, status };
});

