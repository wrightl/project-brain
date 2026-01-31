import { createApiRoute } from '@/_lib/api-route-handler';
import { JournalService, SystemTag } from '@/_services/journal-service';

export const GET = createApiRoute<SystemTag[]>(async () => {
    return JournalService.getSystemTags();
});

