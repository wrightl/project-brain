import { callBackendApi } from '@/_lib/backend-api';

export interface VoiceNote {
    id: string;
    fileName: string;
    audioUrl: string;
    duration: number | null;
    fileSize: number | null;
    description: string | null;
    createdAt: string;
    updatedAt: string;
}

export class VoiceNoteService {
    /**
     * Get all voice notes for the current user
     */
    static async getAllVoiceNotes(limit?: number): Promise<VoiceNote[]> {
        const queryParam = limit ? `?limit=${limit}` : '';
        const response = await callBackendApi(`/voicenotes${queryParam}`, {
            method: 'GET',
        });

        if (!response.ok) {
            throw new Error('Failed to fetch voice notes');
        }

        return response.json();
    }

    /**
     * Upload a voice note
     */
    static async uploadVoiceNote(
        file: File,
        description?: string
    ): Promise<VoiceNote> {
        const formData = new FormData();
        formData.append('file', file);
        if (description && description.trim()) {
            formData.append('description', description.trim());
        }

        const response = await callBackendApi('/voicenotes', {
            method: 'POST',
            body: formData,
            isFormData: true,
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to upload voice note');
        }

        return response.json();
    }

    /**
     * Delete a voice note
     */
    static async deleteVoiceNote(voiceNoteId: string): Promise<void> {
        const response = await callBackendApi(`/voicenotes/${voiceNoteId}`, {
            method: 'DELETE',
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to delete voice note');
        }
    }

    /**
     * Get voice note audio file as a blob
     */
    static async getVoiceNoteAudio(
        voiceNoteId: string
    ): Promise<{ blob: Blob; headers: Headers }> {
        const response = await callBackendApi(
            `/voicenotes/${voiceNoteId}/audio`,
            {
                method: 'GET',
            }
        );

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to fetch voice note audio');
        }

        const blob = await response.blob();

        // Get headers from the response
        const contentType = response.headers.get('Content-Type') || 'audio/m4a';
        const contentDisposition = response.headers.get('Content-Disposition');

        // Prepare headers for the Next.js response
        const headers = new Headers();
        headers.set('Content-Type', contentType);

        if (contentDisposition) {
            headers.set('Content-Disposition', contentDisposition);
        }

        // Support range requests for audio streaming
        const acceptRanges = response.headers.get('Accept-Ranges');
        if (acceptRanges) {
            headers.set('Accept-Ranges', acceptRanges);
        }

        return { blob, headers };
    }
}
