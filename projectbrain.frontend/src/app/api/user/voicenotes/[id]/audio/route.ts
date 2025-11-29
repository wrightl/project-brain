import { NextRequest, NextResponse } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { BackendApiError } from '@/_lib/backend-api';
import { VoiceNoteService } from '@/_services/voicenote-service';

export const GET = createApiRoute(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    // Extract ID from pathname: /api/user/voicenotes/{id}/audio
    const parts = pathname.split('/');
    // Find the index of 'voicenotes' and get the next part as the ID
    const voicenotesIndex = parts.indexOf('voicenotes');
    const id = voicenotesIndex !== -1 ? parts[voicenotesIndex + 1] : undefined;

    if (!id || id === 'audio') {
        throw new BackendApiError(400, 'Voice note ID is required');
    }

    const { blob, headers } = await VoiceNoteService.getVoiceNoteAudio(id);

    if (!blob) {
        throw new BackendApiError(404, 'Voice note audio not found');
    }

    // Return the blob as a NextResponse with proper headers
    return new NextResponse(blob, {
        status: 200,
        headers,
    });
});
