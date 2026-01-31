import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { GoalService } from '@/_services/goal-service';

export const GET = createApiRoute<{
    currentStreak: number;
    longestStreak: number;
}>(async (req: NextRequest) => {
    const summary = await GoalService.getStreakSummary();
    return summary;
});
