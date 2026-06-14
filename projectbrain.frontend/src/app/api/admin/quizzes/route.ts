import { createAdminApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';
import { PagedResponse } from '@/_lib/types';
import { Quiz, QuizService } from '@/_services/quiz-service';

export const GET = createAdminApiRoute<PagedResponse<Quiz>>(
    async (req: NextRequest) => {
        const { searchParams } = new URL(req.url);
        const pageParam = searchParams.get('page');
        const pageSizeParam = searchParams.get('pageSize');

        const options = {
            page: pageParam ? parseInt(pageParam, 10) : undefined,
            pageSize: pageSizeParam ? parseInt(pageSizeParam, 10) : undefined,
        };

        return await QuizService.getAllQuizzes(options);
    }
);

export const POST = createAdminApiRoute(async (request: NextRequest) => {
    const body = await request.json();
    const response = await callBackendApi('/quizes', {
        method: 'POST',
        body,
    });
    if (!response.ok) {
        const errorData = await response.json();
        return Response.json(errorData, { status: response.status });
    }
    const quiz = await response.json();
    return Response.json(quiz, { status: 201 });
});
