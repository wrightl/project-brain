import { createApiRoute } from '@/_lib/api-route-handler';
import { SubscriptionService } from '@/_services/subscription-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<{ tier: string; userType: string }>(
    async (req: NextRequest) => {
        const tierInfo = await SubscriptionService.getTier();
        return tierInfo;
    }
);
