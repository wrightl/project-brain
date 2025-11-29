import { createApiRoute } from '@/_lib/api-route-handler';
import { VoiceNoteService } from '@/_services/voicenote-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const DELETE = createApiRoute(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    const id = pathname.split('/').pop();

    if (!id) {
        throw new BackendApiError(400, 'Voice note ID is required');
    }

    await VoiceNoteService.deleteVoiceNote(id);
    return { success: true };
});

