import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachService } from '@/_services/coach-service';
import { Coach } from '@/_lib/types';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<Coach>(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    const id = pathname.split('/').pop();

    if (!id) {
        throw new BackendApiError(400, 'Coach ID is required');
    }

    const coach = await CoachService.getCoachById(parseInt(id, 10));

    if (!coach) {
        throw new BackendApiError(404, 'Coach not found');
    }

    return coach;
});

