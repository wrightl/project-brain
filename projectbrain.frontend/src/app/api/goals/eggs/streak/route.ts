import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { GoalService } from '@/_services/goal-service';

export const GET = createApiRoute<{ streak: number }>(async (req: NextRequest) => {
    const streak = await GoalService.getCompletionStreak();
    return { streak };
});

