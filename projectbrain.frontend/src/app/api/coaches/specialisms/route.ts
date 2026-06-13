import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachService } from '@/_services/coach-service';

export const GET = createApiRoute<string[]>(async () => {
    return CoachService.getSpecialisms();
});
