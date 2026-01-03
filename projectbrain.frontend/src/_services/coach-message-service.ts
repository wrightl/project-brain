import { callBackendApi } from '@/_lib/backend-api';

export interface CoachMessage {
    id: string;
    // userId: string;
    // coachId: string;
    connectionId?: string; // Added for SignalR message updates
    senderId: string;
    messageType: 'text' | 'voice';
    content: string;
    voiceNoteUrl?: string;
    voiceNoteFileName?: string;
    status: 'sent' | 'delivered' | 'read';
    deliveredAt?: string;
    readAt?: string;
    createdAt: string;
    sender?: {
        // id: string;
        fullName: string;
        email: string;
    };
}

export interface SendMessageRequest {
    userId: string;
    coachId: string;
    content: string;
}

export class CoachMessageService {
    /**
     * Get conversation messages between a user and coach
     */
    static async getConversationMessages(
        userId: string,
        coachId: string,
        pageSize: number = 20,
        beforeDate?: Date
    ): Promise<CoachMessage[]> {
        const params = new URLSearchParams();
        params.append('pageSize', pageSize.toString());
        if (beforeDate) {
            params.append('beforeDate', beforeDate.toISOString());
        }

        const response = await callBackendApi(
            `/coach-messages/conversation/${userId}/${coachId}?${params.toString()}`
        );
        if (!response.ok) {
            throw new Error('Failed to fetch conversation messages');
        }
        return response.json();
    }

    /**
     * Send a text message
     */
    static async sendMessage(
        request: SendMessageRequest
    ): Promise<CoachMessage> {
        const response = await callBackendApi('/coach-messages', {
            method: 'POST',
            body: request,
        });
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(
                errorData.error?.message || 'Failed to send message'
            );
        }
        return response.json();
    }

    /**
     * Send a voice message
     */
    static async sendVoiceMessage(
        connectionId: string,
        audioFile: File
    ): Promise<CoachMessage> {
        const formData = new FormData();
        formData.append('file', audioFile);
        formData.append('connectionId', connectionId);

        const response = await callBackendApi('/coach-messages/voice', {
            method: 'POST',
            body: formData,
            isFormData: true,
        });
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(
                errorData.error?.message || 'Failed to send voice message'
            );
        }
        return response.json();
    }

    /**
     * Mark a message as delivered
     */
    static async markAsDelivered(messageId: string): Promise<void> {
        const response = await callBackendApi(
            `/coach-messages/${messageId}/delivered`,
            {
                method: 'PUT',
            }
        );
        if (!response.ok) {
            throw new Error('Failed to mark message as delivered');
        }
    }

    /**
     * Mark a message as read
     */
    static async markAsRead(messageId: string): Promise<void> {
        const response = await callBackendApi(
            `/coach-messages/${messageId}/read`,
            {
                method: 'PUT',
            }
        );
        if (!response.ok) {
            throw new Error('Failed to mark message as read');
        }
    }

    /**
     * Mark entire conversation as read
     */
    static async markConversationAsRead(
        userId: string,
        coachId: string
    ): Promise<void> {
        const response = await callBackendApi(
            `/coach-messages/conversation/${userId}/${coachId}/read`,
            {
                method: 'PUT',
            }
        );
        if (!response.ok) {
            throw new Error('Failed to mark conversation as read');
        }
    }

    /**
     * Search messages in a conversation
     */
    static async searchMessages(
        userId: string,
        coachId: string,
        searchTerm: string
    ): Promise<CoachMessage[]> {
        const params = new URLSearchParams();
        params.append('searchTerm', searchTerm);

        const response = await callBackendApi(
            `/coach-messages/conversation/${userId}/${coachId}/search?${params.toString()}`
        );
        if (!response.ok) {
            throw new Error('Failed to search messages');
        }
        return response.json();
    }

    /**
     * Delete a message
     */
    static async deleteMessage(messageId: string): Promise<void> {
        const response = await callBackendApi(`/coach-messages/${messageId}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            throw new Error('Failed to delete message');
        }
    }

    /**
     * Get all conversations for the current user with last message and unread count
     */
    static async getConversations(): Promise<ConversationSummary[]> {
        const response = await callBackendApi('/coach-messages/conversations');
        if (!response.ok) {
            throw new Error('Failed to fetch conversations');
        }
        return response.json();
    }
}

export interface ConversationSummary {
    connectionId: string;
    otherPersonName: string;
    otherPersonId: string;
    lastMessageSnippet?: string;
    lastMessageSenderName?: string;
    lastMessageTimestamp?: string;
    unreadCount: number;
}
