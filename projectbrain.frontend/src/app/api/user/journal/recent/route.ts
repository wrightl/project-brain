import { createApiRoute } from '@/_lib/api-route-handler';
import { JournalService, JournalEntry } from '@/_services/journal-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<JournalEntry[]>(async (req: NextRequest) => {
    const { searchParams } = new URL(req.url);
    const countParam = searchParams.get('count');
    const count = countParam ? parseInt(countParam, 10) : 3;

    const result = await JournalService.getRecentJournalEntries(count);
    return result;
});

