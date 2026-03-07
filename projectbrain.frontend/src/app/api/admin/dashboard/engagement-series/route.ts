import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { AdminDashboardService } from '@/_services/admin-dashboard-service';

export const GET = createApiRoute<
    { date: string; count: number }[]
>(async (req: NextRequest) => {
    const { searchParams } = req.nextUrl;
    const metric = (searchParams.get('metric') as 'conversations' | 'quiz-responses') || 'conversations';
    const days = Math.min(90, Math.max(1, parseInt(searchParams.get('days') || '14', 10) || 14));

    const data = await AdminDashboardService.getEngagementSeries(metric, days);
    return data;
});
