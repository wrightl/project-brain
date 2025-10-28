import {
    User,
    OnboardingData,
    CoachOnboardingData,
    UserOnboardingData,
} from '@/_lib/types';
import { Conversation, ChatMessage, UploadResult } from '@/_lib/types';
import { getAccessToken } from './auth';

const API_URL = process.env.API_SERVER_URL || 'https://localhost:7585';

/**
 * Custom API Error class for better error handling
 */
export class ApiError extends Error {
    constructor(
        public status: number,
        message: string,
        public response?: Response
    ) {
        super(message);
        this.name = 'ApiError';
    }

    static isApiError(error: unknown): error is ApiError {
        return error instanceof ApiError;
    }
}

/**
 * Get authorization headers with Bearer token
 * @param accessToken - Optional access token to use. If not provided, fetches from API route
 */
async function getAuthHeaders(accessToken: string): Promise<HeadersInit> {
    const token = accessToken;

    if (!token) {
        throw new Error('No access token available');
    }

    return {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
    };
}

/**
 * Generic API call wrapper with error handling
 */
async function apiCall<T>(
    endpoint: string,
    options: RequestInit = {}
): Promise<T> {
    try {
        const token = await getAccessToken();

        const headers = await getAuthHeaders(token);
        const url = `${API_URL}${endpoint}`;

        const response = await fetch(url, {
            ...options,
            headers: {
                ...headers,
                ...options.headers,
            },
        });

        if (!response.ok) {
            let errorMessage = `API Error (${response.status})`;

            // Try to get error details from response
            try {
                const contentType = response.headers.get('content-type');
                if (contentType?.includes('application/json')) {
                    const errorData = await response.json();
                    errorMessage =
                        errorData.message || errorData.error || errorMessage;
                } else {
                    const errorText = await response.text();
                    if (errorText)
                        errorMessage = `${errorMessage}: ${errorText}`;
                }
            } catch {
                // If parsing fails, use the default message
            }

            throw new ApiError(response.status, errorMessage, response);
        }

        // Handle 204 No Content
        if (response.status === 204) {
            return {} as T;
        }

        return response.json();
    } catch (error) {
        // Re-throw ApiError as-is
        if (ApiError.isApiError(error)) {
            throw error;
        }

        // Handle network errors and other exceptions
        if (error instanceof TypeError && error.message === 'Failed to fetch') {
            throw new ApiError(0, 'Network error: Unable to reach the server');
        }

        // Re-throw other errors
        throw error;
    }
}

// ============================================================================
// User API
// ============================================================================

export async function getCurrentUser(): Promise<User | null> {
    try {
        return await apiCall<User>('/users/me');
    } catch (error) {
        console.error('Failed to get current user:', error);
        return null;
    }
}

export async function onboardUser(
    data: OnboardingData | CoachOnboardingData | UserOnboardingData
): Promise<User> {
    return apiCall<User>('/users/me/onboarding', {
        method: 'POST',
        body: JSON.stringify(data),
    });
}

export async function getUserByEmail(email: string): Promise<User | null> {
    try {
        return await apiCall<User>(`/users/${email}`);
    } catch (error) {
        console.error('Failed to get user by email:', error);
        return null;
    }
}

// ============================================================================
// Conversation API
// ============================================================================

export async function getConversations(): Promise<Conversation[]> {
    return apiCall<Conversation[]>('/conversation');
}

export async function getConversation(
    id: string
): Promise<Conversation | null> {
    try {
        return await apiCall<Conversation>(`/conversation/${id}`);
    } catch (error) {
        console.error('Failed to get conversation:', error);
        return null;
    }
}

export async function getConversationWithMessages(
    id: string
): Promise<Conversation | null> {
    try {
        return await apiCall<Conversation>(`/conversation/${id}/full`);
    } catch (error) {
        console.error('Failed to get conversation with messages:', error);
        return null;
    }
}

export async function getConversationMessages(
    conversationId: string
): Promise<ChatMessage[]> {
    return apiCall<ChatMessage[]>(`/conversation/${conversationId}/messages`);
}

export async function createConversation(title: string): Promise<Conversation> {
    return apiCall<Conversation>('/conversation', {
        method: 'POST',
        body: JSON.stringify({ title }),
    });
}

export async function updateConversation(
    id: string,
    title: string
): Promise<Conversation> {
    return apiCall<Conversation>(`/conversation/${id}`, {
        method: 'PUT',
        body: JSON.stringify({ title }),
    });
}

export async function deleteConversation(id: string): Promise<void> {
    return apiCall<void>(`/conversation/${id}`, {
        method: 'DELETE',
    });
}

// ============================================================================
// Chat API
// ============================================================================

/**
 * Stream chat response using Server-Sent Events
 */
export async function streamChat(
    content: string,
    conversationId?: string,
    onChunk?: (text: string) => void,
    onConversationId?: (id: string) => void
): Promise<void> {
    const accessToken = await getAccessToken();

    if (!accessToken) {
        throw new ApiError(401, 'No access token available');
    }

    let response: Response;
    try {
        response = await fetch(`${API_URL}/chat/stream`, {
            method: 'POST',
            headers: {
                Authorization: `Bearer ${accessToken}`,
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ content, conversationId }),
        });
    } catch (error) {
        if (error instanceof TypeError && error.message === 'Failed to fetch') {
            throw new ApiError(0, 'Network error: Unable to reach the server');
        }
        throw error;
    }

    if (!response.ok) {
        let errorMessage = `Chat stream failed (${response.status})`;
        try {
            const errorText = await response.text();
            if (errorText) errorMessage = `${errorMessage}: ${errorText}`;
        } catch {
            // Use default message if parsing fails
        }
        throw new ApiError(response.status, errorMessage, response);
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
        throw new ApiError(500, 'Response body is not readable');
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
                        if (parsed.value && onChunk) {
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
 * Upload knowledge files
 */
export async function uploadKnowledgeFiles(
    files: File[]
): Promise<UploadResult[]> {
    const accessToken = await getAccessToken();

    if (!accessToken) {
        throw new ApiError(401, 'No access token available');
    }

    if (files.length === 0) {
        throw new ApiError(400, 'No files provided for upload');
    }

    const formData = new FormData();
    files.forEach((file) => {
        formData.append('files', file);
    });

    let response: Response;
    try {
        response = await fetch(`${API_URL}/chat/knowledge/upload`, {
            method: 'POST',
            headers: {
                Authorization: `Bearer ${accessToken}`,
            },
            body: formData,
        });
    } catch (error) {
        if (error instanceof TypeError && error.message === 'Failed to fetch') {
            throw new ApiError(0, 'Network error: Unable to reach the server');
        }
        throw error;
    }

    if (!response.ok) {
        let errorMessage = `Upload failed (${response.status})`;
        try {
            const errorText = await response.text();
            if (errorText) errorMessage = `${errorMessage}: ${errorText}`;
        } catch {
            // Use default message if parsing fails
        }
        throw new ApiError(response.status, errorMessage, response);
    }

    return response.json();
}
