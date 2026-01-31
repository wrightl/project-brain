import { createApiRoute } from '@/_lib/api-route-handler';
import { UserService, TimezoneResponse } from '@/_services/user-service';
import { NextRequest } from 'next/server';
import { BackendApiError } from '@/_lib/backend-api';

export const GET = createApiRoute<TimezoneResponse>(async () => {
    return UserService.getTimezone();
});

export const PUT = createApiRoute<TimezoneResponse>(async (req: NextRequest) => {
    const body = await req.json();
    const { timezone } = body;

    if (!timezone || typeof timezone !== 'string') {
        throw new BackendApiError(400, 'Timezone is required');
    }

    return UserService.updateTimezone(timezone);
});

