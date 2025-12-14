import { createApiRoute } from '@/_lib/api-route-handler';
import { QuizService, Quiz } from '@/_services/quiz-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<Quiz>(
    async (req: NextRequest, { params }: { params: Promise<{ quizId: string }> }) => {
        const { quizId } = await params;
        const result = await QuizService.getQuizById(quizId);
        return result;
    }
);

