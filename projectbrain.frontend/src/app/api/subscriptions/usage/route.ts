import { createApiRoute } from '@/_lib/api-route-handler';
import { SubscriptionService, Usage } from '@/_services/subscription-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<Usage>(async (req: NextRequest) => {
    const usage = await SubscriptionService.getUsage();
    return usage;
});

