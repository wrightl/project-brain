import { createApiRoute } from '@/_lib/api-route-handler';
import { NextRequest, NextResponse } from 'next/server';
import { PagedResponse } from '@/_lib/types';
import { Quiz, QuizService } from '@/_services/quiz-service';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createApiRoute<PagedResponse<Quiz>>(
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

export async function POST(request: NextRequest) {
    try {
        const body = await request.json();
        const response = await callBackendApi('/quizes', {
            method: 'POST',
            body,
        });
        if (!response.ok) {
            const errorData = await response.json();
            return NextResponse.json(errorData, { status: response.status });
        }
        const quiz = await response.json();
        return NextResponse.json(quiz, { status: 201 });
    } catch (error) {
        console.error('Error creating quiz:', error);
        return NextResponse.json(
            { error: 'Internal server error' },
            { status: 500 }
        );
    }
}
