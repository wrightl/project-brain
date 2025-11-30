import { createApiRoute } from '@/_lib/api-route-handler';
import { SubscriptionService, Subscription } from '@/_services/subscription-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<Subscription>(async (req: NextRequest) => {
    const subscription = await SubscriptionService.getMySubscription();
    return subscription;
});

