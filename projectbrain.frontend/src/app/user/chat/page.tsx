'use client';

import { useState, useRef, useEffect, useCallback } from 'react';
import { streamChat } from '@/lib/api-client';
import { ChatMessage } from '@/types/chat';

export default function ChatPage() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [conversationId, setConversationId] = useState<string | undefined>();
  const [error, setError] = useState<string | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  // Auto-scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Auto-resize textarea
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = textareaRef.current.scrollHeight + 'px';
    }
  }, [input]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, []);

  const handleSendMessage = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();

    if (!input.trim() || isStreaming) return;

    const userMessage: ChatMessage = {
      role: 'user',
      content: input.trim(),
      createdAt: new Date().toISOString(),
    };

    // Add user message
    setMessages((prev) => [...prev, userMessage]);
    setInput('');
    setIsStreaming(true);
    setError(null);

    // Create placeholder for assistant message
    const assistantMessage: ChatMessage = {
      role: 'assistant',
      content: '',
      createdAt: new Date().toISOString(),
    };
    setMessages((prev) => [...prev, assistantMessage]);

    // Create new abort controller for this request
    abortControllerRef.current = new AbortController();

    try {
      await streamChat(
        userMessage.content,
        conversationId,
        // onChunk callback
        (chunk: string) => {
          setMessages((prev) => {
            const newMessages = [...prev];
            const lastMessage = newMessages[newMessages.length - 1];
            if (lastMessage && lastMessage.role === 'assistant') {
              lastMessage.content += chunk;
            }
            return newMessages;
          });
        },
        // onConversationId callback
        (id: string) => {
          setConversationId(id);
        }
      );
    } catch (err) {
      console.error('Chat error:', err);
      const errorMessage = err instanceof Error ? err.message : 'Failed to send message';
      setError(errorMessage);
      // Remove the failed assistant message
      setMessages((prev) => prev.slice(0, -1));
    } finally {
      setIsStreaming(false);
      abortControllerRef.current = null;
    }
  }, [input, isStreaming, conversationId]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage(e);
    }
  }, [handleSendMessage]);

  return (
    <div className="flex flex-col h-screen bg-gray-50 dark:bg-gray-900">
      {/* Header */}
      <header className="flex-none bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-6 py-4">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
          Chat Assistant
        </h1>
        {conversationId && (
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Conversation: {conversationId}
          </p>
        )}
      </header>

      {/* Messages Container */}
      <main
        className="flex-1 overflow-y-auto px-4 py-6"
        role="log"
        aria-live="polite"
        aria-label="Chat messages">
        <div className="max-w-4xl mx-auto space-y-6">
          {messages.length === 0 ? (
            <div className="text-center text-gray-500 dark:text-gray-400 py-12">
              <svg
                className="mx-auto h-12 w-12 mb-4"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
                />
              </svg>
              <p className="text-lg font-medium">Start a conversation</p>
              <p className="text-sm mt-2">
                Send a message to begin chatting with the AI assistant
              </p>
            </div>
          ) : (
            messages.map((message, index) => (
              <article
                key={index}
                className={`flex ${
                  message.role === 'user' ? 'justify-end' : 'justify-start'
                }`}
                aria-label={`${message.role === 'user' ? 'User' : 'Assistant'} message`}
              >
                <div
                  className={`max-w-3xl rounded-lg px-4 py-3 ${
                    message.role === 'user'
                      ? 'bg-blue-600 text-white'
                      : 'bg-white dark:bg-gray-800 text-gray-900 dark:text-white border border-gray-200 dark:border-gray-700'
                  }`}
                >
                  <div className="flex items-start space-x-2">
                    {message.role === 'assistant' && (
                      <svg
                        className="h-5 w-5 mt-0.5 flex-shrink-0 text-blue-600 dark:text-blue-400"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                        />
                      </svg>
                    )}
                    <div className="flex-1 whitespace-pre-wrap break-words">
                      {message.content || (
                        <span className="text-gray-400 italic">
                          Thinking...
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              </article>
            ))
          )}
          <div ref={messagesEndRef} />
        </div>
      </main>

      {/* Error Display */}
      {error && (
        <aside
          role="alert"
          aria-live="assertive"
          className="flex-none bg-red-50 dark:bg-red-900/20 border-t border-red-200 dark:border-red-800 px-6 py-3"
        >
          <div className="max-w-4xl mx-auto flex items-center space-x-2 text-red-600 dark:text-red-400">
            <svg
              className="h-5 w-5 flex-shrink-0"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <span className="text-sm">{error}</span>
          </div>
        </aside>
      )}

      {/* Input Form */}
      <footer className="flex-none bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700 px-6 py-4">
        <form
          onSubmit={handleSendMessage}
          className="max-w-4xl mx-auto"
          aria-label="Send message form"
        >
          <div className="flex items-end space-x-4">
            <div className="flex-1 relative">
              <textarea
                ref={textareaRef}
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="Type your message... (Shift+Enter for new line)"
                disabled={isStreaming}
                rows={1}
                aria-label="Message input"
                aria-describedby="input-hint"
                className="w-full resize-none rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-4 py-3 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent disabled:opacity-50 disabled:cursor-not-allowed max-h-32 overflow-y-auto"
                style={{ minHeight: '48px' }}
              />
            </div>
            <button
              type="submit"
              disabled={!input.trim() || isStreaming}
              aria-label={isStreaming ? 'Sending message' : 'Send message'}
              className="flex-shrink-0 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-300 dark:disabled:bg-gray-600 disabled:cursor-not-allowed text-white font-medium px-6 py-3 rounded-lg transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 h-12"
            >
              {isStreaming ? (
                <svg
                  className="animate-spin h-5 w-5"
                  fill="none"
                  viewBox="0 0 24 24"
                >
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  />
                </svg>
              ) : (
                <svg
                  className="h-5 w-5"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"
                  />
                </svg>
              )}
            </button>
          </div>
          <p
            id="input-hint"
            className="text-xs text-gray-500 dark:text-gray-400 mt-2"
          >
            Press Enter to send, Shift+Enter for new line
          </p>
        </form>
      </footer>
    </div>
  );
}
