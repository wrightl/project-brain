import { createApiRoute } from '@/_lib/api-route-handler';
import { SubscriptionService } from '@/_services/subscription-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const POST = createApiRoute<{ url: string }>(async (req: NextRequest) => {
    const body = await req.json();
    const { tier, isAnnual } = body;

    if (!tier || typeof isAnnual !== 'boolean') {
        throw new BackendApiError(400, 'Missing required fields: tier, isAnnual');
    }

    const result = await SubscriptionService.createCheckout(tier, isAnnual);
    return result;
});

