export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  createdAt?: string;
}

export interface Conversation {
  id: string;
  userId: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  messages?: ChatMessage[];
}

export interface ChatRequest {
  conversationId?: string;
  content: string;
}

export interface ChatStreamChunk {
  type: 'text';
  value: string;
}

export interface UploadResult {
  status: 'uploaded' | 'error';
  filename: string;
  chunks?: number;
  message?: string;
}
