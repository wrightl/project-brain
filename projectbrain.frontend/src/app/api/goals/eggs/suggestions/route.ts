import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import {
    GoalService,
    type GoalSuggestionsResponse,
} from '@/_services/goal-service';

export const GET = createApiRoute<GoalSuggestionsResponse>(
    async (_req: NextRequest) => {
        const data = await GoalService.getSuggestions();
        return data;
    },
);
