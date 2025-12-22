import { NextRequest, NextResponse } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { GoalService } from '@/_services/goal-service';
import { Goal } from '@/_services/goal-service';

export const GET = createApiRoute<Goal[]>(async (req: NextRequest) => {
    const goals = await GoalService.getTodaysGoals();
    return goals;
});

export const POST = createApiRoute<Goal[]>(async (req: NextRequest) => {
    const body = await req.json();
    const { goals } = body;

    if (!goals || !Array.isArray(goals) || goals.length === 0 || goals.length > 3) {
        throw new Error('Goals must be an array with 1-3 items');
    }

    const result = await GoalService.createOrUpdateGoals(goals);
    return result;
});
