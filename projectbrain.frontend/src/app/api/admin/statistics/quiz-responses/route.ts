import { NextRequest } from 'next/server';
import { createAdminApiRoute } from '@/_lib/api-route-handler';
import { StatisticsService } from '@/_services/statistics-service';

export const GET = createAdminApiRoute<{ count: number; period?: string }>(
    async (req: NextRequest) => {
        const { searchParams } = req.nextUrl;
        const period = searchParams.get('period') || undefined;

        const count = await StatisticsService.getQuizResponses(
            period as any
        );

        return { count, period: period || undefined };
    }
);

