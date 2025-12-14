import { createApiRoute } from '@/_lib/api-route-handler';
import { VoiceNoteService, VoiceNote } from '@/_services/voicenote-service';
import { BackendApiError } from '@/_lib/backend-api';
import { PagedResponse } from '@/_lib/types';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<PagedResponse<VoiceNote>>(
    async (req: NextRequest) => {
        const { searchParams } = new URL(req.url);
        const pageParam = searchParams.get('page');
        const pageSizeParam = searchParams.get('pageSize');

        const options = {
            page: pageParam ? parseInt(pageParam, 10) : undefined,
            pageSize: pageSizeParam ? parseInt(pageSizeParam, 10) : undefined,
        };

        const result = await VoiceNoteService.getAllVoiceNotes(options);
        return result;
    }
);

export const POST = createApiRoute<VoiceNote>(async (req: NextRequest) => {
    const formData = await req.formData();
    const file = formData.get('file') as File;
    const description = formData.get('description') as string | null;

    if (!file) {
        throw new BackendApiError(400, 'No file provided');
    }

    const voiceNote = await VoiceNoteService.uploadVoiceNote(
        file,
        description || undefined
    );
    return voiceNote;
});
