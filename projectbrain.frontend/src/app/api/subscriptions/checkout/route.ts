import { createApiRoute } from '@/_lib/api-route-handler';
import { SubscriptionService } from '@/_services/subscription-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const POST = createApiRoute<{ url: string }>(
    async (req: NextRequest) => {
        const body = await req.json();
        const { tier, isAnnual } = body;

        if (!tier || typeof isAnnual !== 'boolean') {
            throw new BackendApiError(
                400,
                'Missing required fields: tier, isAnnual',
            );
        }

        // Extract origin from request
        const origin =
            req.headers.get('origin') ||
            req.nextUrl.origin ||
            'https://localhost:3000';

        const result = await SubscriptionService.createCheckout(
            tier,
            isAnnual,
            origin,
        );
        return result;
    },
);
