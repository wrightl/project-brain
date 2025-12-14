import { createApiRoute } from '@/_lib/api-route-handler';
import { QuizService } from '@/_services/quiz-service';

export const GET = createApiRoute<{ count: number }>(async () => {
    const result = await QuizService.getQuizResponseCount();
    return result;
});

