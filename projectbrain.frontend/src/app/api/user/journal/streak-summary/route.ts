import { createApiRoute } from '@/_lib/api-route-handler';
import { JournalService, JournalStreakSummary } from '@/_services/journal-service';

export const GET = createApiRoute<JournalStreakSummary>(async () => {
    return JournalService.getJournalStreakSummary();
});

