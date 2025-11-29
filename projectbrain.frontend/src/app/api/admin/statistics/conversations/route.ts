import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { StatisticsService } from '@/_services/statistics-service';

export const GET = createApiRoute<{ count: number; period?: string }>(
    async (req: NextRequest) => {
        const { searchParams } = req.nextUrl;
        const period = searchParams.get('period') || undefined;

        const count = await StatisticsService.getAllConversations(
            period as any
        );

        return { count, period: period || undefined };
    }
);
