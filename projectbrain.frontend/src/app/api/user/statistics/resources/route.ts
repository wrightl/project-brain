import { createApiRoute } from '@/_lib/api-route-handler';
import { StatisticsService } from '@/_services/statistics-service';

export const GET = createApiRoute<{ count: number }>(async () => {
    const count = await StatisticsService.getUserResources();
    return { count };
});

