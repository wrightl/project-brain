import { createApiRoute } from '@/_lib/api-route-handler';
import { VoiceNoteService, VoiceNote } from '@/_services/voicenote-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<VoiceNote[]>(async (req: NextRequest) => {
    const { searchParams } = new URL(req.url);
    const limitParam = searchParams.get('limit');
    const limit = limitParam ? parseInt(limitParam, 10) : undefined;

    const voiceNotes = await VoiceNoteService.getAllVoiceNotes(limit);
    return voiceNotes;
});

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
