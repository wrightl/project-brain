import { createApiRoute } from '@/_lib/api-route-handler';
import { QuizService, QuizResponse } from '@/_services/quiz-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const POST = createApiRoute<QuizResponse>(
    async (req: NextRequest, { params }: { params: Promise<{ quizId: string }> }) => {
        const { quizId } = await params;
        const body = await req.json();
        const { answers, completedAt } = body;

        if (!answers || typeof answers !== 'object') {
            throw new BackendApiError(400, 'Answers are required');
        }

        const result = await QuizService.submitQuizResponse(quizId, answers, completedAt);
        return result;
    }
);

