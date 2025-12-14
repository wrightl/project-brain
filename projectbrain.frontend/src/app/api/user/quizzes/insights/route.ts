import { createApiRoute } from '@/_lib/api-route-handler';
import { QuizService } from '@/_services/quiz-service';

export const GET = createApiRoute<{
    summary: string;
    keyInsights: string[];
    lastUpdated: string;
}>(async () => {
    const result = await QuizService.getQuizInsights();
    return result;
});

