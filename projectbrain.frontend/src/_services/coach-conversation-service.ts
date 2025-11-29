import { callBackendApi } from '@/_lib/backend-api';
import { Conversation } from '@/_lib/types';

export class CoachConversationService {
    /**
     * Get all conversations for the current user
     */
    static async getConversations(): Promise<Conversation[]> {
        const response = await callBackendApi('/conversation');
        return response.json();
    }

    /**
     * Get conversation by ID with messages
     */
    static async getConversationWithMessages(
        id: string
    ): Promise<Conversation | null> {
        try {
            const response = await callBackendApi(`/conversation/${id}/full`);
            if (!response.ok) {
                if (response.status === 404) {
                    return null;
                }
                throw new Error(
                    `Failed to fetch conversation: ${response.status}`
                );
            }
            return response.json();
        } catch (error) {
            console.error('Error fetching conversation with messages:', error);
            return null;
        }
    }

    /**
     * Get conversation by ID (without messages)
     */
    static async getConversation(id: string): Promise<Conversation | null> {
        try {
            const response = await callBackendApi(`/conversation/${id}`);
            if (!response.ok) {
                if (response.status === 404) {
                    return null;
                }
                throw new Error(
                    `Failed to fetch conversation: ${response.status}`
                );
            }
            return response.json();
        } catch (error) {
            console.error('Error fetching conversation:', error);
            return null;
        }
    }

    /**
     * Create a new conversation
     */
    static async createConversation(title: string): Promise<Conversation> {
        const response = await callBackendApi('/conversation', {
            method: 'POST',
            body: { title },
        });
        if (!response.ok) {
            throw new Error(
                `Failed to create conversation: ${response.status}`
            );
        }
        return response.json();
    }

    /**
     * Delete a conversation by ID
     */
    static async deleteConversation(id: string): Promise<void> {
        const response = await callBackendApi(`/conversation/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            throw new Error(
                `Failed to delete conversation: ${response.status}`
            );
        }
    }
}
