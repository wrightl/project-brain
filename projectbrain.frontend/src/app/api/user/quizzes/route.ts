import { createApiRoute } from '@/_lib/api-route-handler';
import { QuizService, Quiz } from '@/_services/quiz-service';
import { PagedResponse } from '@/_lib/types';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<PagedResponse<Quiz>>(
    async (req: NextRequest) => {
        const { searchParams } = new URL(req.url);
        const pageParam = searchParams.get('page');
        const pageSizeParam = searchParams.get('pageSize');

        const options = {
            page: pageParam ? parseInt(pageParam, 10) : undefined,
            pageSize: pageSizeParam ? parseInt(pageSizeParam, 10) : undefined,
        };

        const result = await QuizService.getAllQuizzes(options);
        return result;
    }
);

