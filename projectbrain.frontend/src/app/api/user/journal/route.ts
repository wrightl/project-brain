import { createApiRoute } from '@/_lib/api-route-handler';
import { JournalService, JournalEntry } from '@/_services/journal-service';
import { BackendApiError } from '@/_lib/backend-api';
import { PagedResponse } from '@/_lib/types';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<PagedResponse<JournalEntry>>(
    async (req: NextRequest) => {
        const { searchParams } = new URL(req.url);
        const pageParam = searchParams.get('page');
        const pageSizeParam = searchParams.get('pageSize');

        const options = {
            page: pageParam ? parseInt(pageParam, 10) : undefined,
            pageSize: pageSizeParam ? parseInt(pageSizeParam, 10) : undefined,
        };

        const result = await JournalService.getAllJournalEntries(options);
        return result;
    }
);

export const POST = createApiRoute<JournalEntry>(async (req: NextRequest) => {
    const body = await req.json();
    const { content, tagIds } = body;

    if (!content || typeof content !== 'string') {
        throw new BackendApiError(400, 'Content is required');
    }

    const journalEntry = await JournalService.createJournalEntry({
        content,
        tagIds,
    });
    return journalEntry;
});

