import { createApiRoute } from '@/_lib/api-route-handler';
import { QuizService, QuizResponse } from '@/_services/quiz-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<QuizResponse>(
    async (req: NextRequest, { params }: { params: Promise<{ id: string }> }) => {
        const { id } = await params;
        const result = await QuizService.getQuizResponseById(id);
        return result;
    }
);

export const DELETE = createApiRoute<void>(
    async (req: NextRequest, { params }: { params: Promise<{ id: string }> }) => {
        const { id } = await params;
        await QuizService.deleteQuizResponse(id);
    }
);
