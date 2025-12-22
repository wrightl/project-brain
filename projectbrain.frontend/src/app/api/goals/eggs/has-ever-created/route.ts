import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { GoalService } from '@/_services/goal-service';

export const GET = createApiRoute<{ hasEverCreated: boolean }>(async (req: NextRequest) => {
    const hasEverCreated = await GoalService.hasEverCreatedGoals();
    return { hasEverCreated };
});

