import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { GoalService } from '@/_services/goal-service';
import { Goal } from '@/_services/goal-service';

export async function POST(
    req: NextRequest,
    context: { params: Promise<{ index: string }> }
) {
    const params = await context.params;
    const index = parseInt(params.index, 10);
    
    return createApiRoute<Goal[]>(async () => {
        if (isNaN(index) || index < 0 || index > 2) {
            throw new Error('Index must be 0, 1, or 2');
        }

        const body = await req.json();
        const { completed } = body;

        if (typeof completed !== 'boolean') {
            throw new Error('Completed must be a boolean');
        }

        const result = await GoalService.completeGoal(index, completed);
        return result;
    })(req);
}
