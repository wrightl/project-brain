import { createApiRoute } from '@/_lib/api-route-handler';
import { JournalService, JournalEntry } from '@/_services/journal-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<JournalEntry>(
    async (req: NextRequest, { params }: { params: { id: string } }) => {
        const journalEntry = await JournalService.getJournalEntry(
            (
                await params
            ).id
        );
        return journalEntry;
    }
);

export const PUT = createApiRoute<JournalEntry>(
    async (req: NextRequest, { params }: { params: { id: string } }) => {
        const body = await req.json();
        const { content, tagIds } = body;

        if (!content || typeof content !== 'string') {
            throw new BackendApiError(400, 'Content is required');
        }

        const journalEntry = await JournalService.updateJournalEntry(
            (
                await params
            ).id,
            {
                content,
                tagIds,
            }
        );
        return journalEntry;
    }
);

export const DELETE = createApiRoute<void>(
    async (req: NextRequest, { params }: { params: { id: string } }) => {
        await JournalService.deleteJournalEntry((await params).id);
    }
);
