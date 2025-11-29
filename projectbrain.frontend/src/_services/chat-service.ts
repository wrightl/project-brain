import { BackendApiError, callBackendApi } from '@/_lib/backend-api';
import { Conversation, Citation } from '@/_lib/types';
import { ConversationService } from './conversation-service';

export class ChatService {
    /**
     * Get current user conversations
     */
    static async getConversations(): Promise<Conversation[]> {
        return ConversationService.getConversations();
    }

    /**
     * Stream chat response using Server-Sent Events from voice input
     */
    static async streamVoiceChat(
        audioBlob: Blob,
        conversationId?: string,
        onChunk?: (text: string) => void,
        onConversationId?: (id: string) => void,
        onTranscription?: (text: string) => void
    ): Promise<void> {
        const formData = new FormData();
        formData.append('audio', audioBlob, 'audio.wav');
        if (conversationId) {
            formData.append('conversationId', conversationId);
        }

        const response = await callBackendApi('/chat/voice', {
            method: 'POST',
            body: formData,
            isFormData: true,
        });

        if (!response.ok) {
            let errorMessage = `Voice chat stream failed (${response.status})`;
            try {
                const errorText = await response.text();
                if (errorText) errorMessage = `${errorMessage}: ${errorText}`;
            } catch {
                // Use default message if parsing fails
            }
            throw new BackendApiError(response.status, errorMessage, response);
        }

        // Get conversation ID from response header
        const newConversationId = response.headers.get('X-Conversation-Id');
        if (newConversationId && onConversationId) {
            onConversationId(newConversationId);
        }

        // Stream the response
        const reader = response.body?.getReader();
        const decoder = new TextDecoder();

        if (!reader) {
            throw new BackendApiError(500, 'Response body is not readable');
        }

        try {
            let isFirstChunk = true;
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                const text = decoder.decode(value, { stream: true });
                const lines = text.split('\n');

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.slice(6);
                        try {
                            const parsed = JSON.parse(data);
                            if (parsed.value && onChunk) {
                                // For voice chat, the first chunk might contain the transcription
                                if (isFirstChunk && onTranscription) {
                                    onTranscription(parsed.value);
                                    isFirstChunk = false;
                                }
                                onChunk(parsed.value);
                            }
                        } catch {
                            // Ignore parse errors for malformed chunks
                        }
                    }
                }
            }
        } finally {
            reader.releaseLock();
        }
    }

    /**
     * Stream chat response using Server-Sent Events
     */
    static async streamChat(
        content: string,
        conversationId?: string,
        onChunk?: (text: string) => void,
        onConversationId?: (id: string) => void,
        onCitations?: (citations: Citation[]) => void
    ): Promise<void> {
        const response = await callBackendApi('/chat/stream', {
            body: JSON.stringify({ content, conversationId }),
        });

        if (!response.ok) {
            let errorMessage = `Chat stream failed (${response.status})`;
            try {
                const errorText = await response.text();
                if (errorText) errorMessage = `${errorMessage}: ${errorText}`;
            } catch {
                // Use default message if parsing fails
            }
            throw new BackendApiError(response.status, errorMessage, response);
        }

        // Get conversation ID from response header
        const newConversationId = response.headers.get('X-Conversation-Id');
        if (newConversationId && onConversationId) {
            onConversationId(newConversationId);
        }

        // Stream the response
        const reader = response.body?.getReader();
        const decoder = new TextDecoder();

        if (!reader) {
            throw new BackendApiError(500, 'Response body is not readable');
        }

        try {
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                const text = decoder.decode(value, { stream: true });
                const lines = text.split('\n');

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.slice(6);
                        try {
                            const parsed = JSON.parse(data);
                            if (
                                parsed.type === 'citations' &&
                                parsed.value &&
                                onCitations
                            ) {
                                // Handle citations metadata
                                onCitations(parsed.value);
                            } else if (
                                parsed.value &&
                                parsed.type !== 'citations' &&
                                onChunk
                            ) {
                                // Handle text chunks
                                onChunk(parsed.value);
                            }
                        } catch {
                            // Ignore parse errors for malformed chunks
                        }
                    }
                }
            }
        } finally {
            reader.releaseLock();
        }
    }
}
