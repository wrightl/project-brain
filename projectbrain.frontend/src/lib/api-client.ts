import { getSession } from '@auth0/nextjs-auth0';
import {
  User,
  OnboardingData,
  CoachOnboardingData,
  UserOnboardingData,
} from '@/types/user';
import {
  Conversation,
  ChatMessage,
  UploadResult,
} from '@/types/chat';

const API_URL = process.env.API_SERVER_URL || 'http://localhost:5448';

/**
 * Get authorization headers with Bearer token
 */
async function getAuthHeaders(): Promise<HeadersInit> {
  const session = await getSession();
  const accessToken = session?.accessToken;

  if (!accessToken) {
    throw new Error('No access token available');
  }

  return {
    Authorization: `Bearer ${accessToken}`,
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
  const headers = await getAuthHeaders();
  const url = `${API_URL}${endpoint}`;

  const response = await fetch(url, {
    ...options,
    headers: {
      ...headers,
      ...options.headers,
    },
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(`API Error (${response.status}): ${error}`);
  }

  // Handle 204 No Content
  if (response.status === 204) {
    return {} as T;
  }

  return response.json();
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

export async function getConversation(id: string): Promise<Conversation | null> {
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
  const session = await getSession();
  const accessToken = session?.accessToken;

  if (!accessToken) {
    throw new Error('No access token available');
  }

  const response = await fetch(`${API_URL}/chat/stream`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ content, conversationId }),
  });

  if (!response.ok) {
    throw new Error(`Chat stream failed: ${response.statusText}`);
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
    throw new Error('Response body is not readable');
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
  const session = await getSession();
  const accessToken = session?.accessToken;

  if (!accessToken) {
    throw new Error('No access token available');
  }

  const formData = new FormData();
  files.forEach((file) => {
    formData.append('files', file);
  });

  const response = await fetch(`${API_URL}/chat/knowledge/upload`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    body: formData,
  });

  if (!response.ok) {
    throw new Error(`Upload failed: ${response.statusText}`);
  }

  return response.json();
}
