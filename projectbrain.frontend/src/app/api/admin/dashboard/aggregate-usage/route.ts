import { createApiRoute } from '@/_lib/api-route-handler';
import { AdminDashboardService } from '@/_services/admin-dashboard-service';

export const GET = createApiRoute<
    import('@/_services/admin-dashboard-service').AdminDashboardAggregateResponse
>(async () => {
    const data = await AdminDashboardService.getAggregateUsage();
    return data;
});
