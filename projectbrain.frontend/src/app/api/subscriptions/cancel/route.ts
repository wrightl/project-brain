import { createApiRoute } from '@/_lib/api-route-handler';
import { SubscriptionService } from '@/_services/subscription-service';
import { NextRequest } from 'next/server';

export const POST = createApiRoute<{ message: string }>(async (req: NextRequest) => {
    const result = await SubscriptionService.cancelSubscription();
    return result;
});

