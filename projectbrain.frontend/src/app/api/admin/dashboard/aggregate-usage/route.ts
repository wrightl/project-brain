import { createAdminApiRoute } from '@/_lib/api-route-handler';
import { AdminDashboardService } from '@/_services/admin-dashboard-service';

export const GET = createAdminApiRoute<
    import('@/_services/admin-dashboard-service').AdminDashboardAggregateResponse
>(async () => {
    const data = await AdminDashboardService.getAggregateUsage();
    return data;
});
