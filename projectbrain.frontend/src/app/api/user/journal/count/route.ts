import { createApiRoute } from '@/_lib/api-route-handler';
import { JournalService } from '@/_services/journal-service';

export const GET = createApiRoute<{ count: number }>(async () => {
    const result = await JournalService.getJournalEntryCount();
    return result;
});

