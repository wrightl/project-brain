"use client";

import { useState, useRef, useEffect } from "react";
import { useRouter } from "next/navigation";
import {
    Conversation,
    ChatMessage,
    Citation,
    ToolExecution,
    ActionCard,
    PendingActionConfirmResponse,
    UserChoicePrompt,
} from "@/_lib/types";
import { fetchWithAuth } from "@/_lib/fetch-with-auth";
import { PaperAirplaneIcon, Bars3Icon } from "@heroicons/react/24/outline";
import Link from "next/link";
import VoiceRecorder from "@/_components/VoiceRecorder";
import FeatureGate from "@/_components/feature-gate";
import ConversationsDrawer from "./conversations-drawer";
import { useAgentFeatureEnabled } from "@/_hooks/use-feature-flag";
import ToolExecutionBadge from "./tool-execution-badge";
import ActionCardWidget from "./action-card";
import UserChoiceChips from "./user-choice-chips";
import { isSafeExternalUrl } from "@/_lib/url-security";
import toast from "react-hot-toast";
import { apiClient } from "@/_lib/api-client";
import { useQueryClient } from "@tanstack/react-query";
import { copingStrategyKeys } from "@/_hooks/queries/use-coping-strategies";

interface ChatInterfaceProps {
    conversation?: Conversation;
    mode?: "default" | "strategies";
    displayName?: string;
}

type SuggestedStrategy = {
    title: string;
    description: string;
    iconKey?: string | null;
    articleUrl?: string | null;
};

type SseJsonMessage = {
    type?: string;
    value?: unknown;
};

export default function ChatInterface({
    conversation,
    mode = "default",
    displayName,
}: ChatInterfaceProps) {
    const router = useRouter();
    const agentFeatureEnabled = useAgentFeatureEnabled();
    const queryClient = useQueryClient();
    const isStrategiesMode = mode === "strategies";
    const nameForGreeting = displayName || "there";
    const [messages, setMessages] = useState<ChatMessage[]>(
        conversation?.messages || [],
    );
    const [conversationId, setConversationId] = useState<string | undefined>(
        conversation?.id,
    );
    const [workflowId, setWorkflowId] = useState<string | undefined>(undefined);
    const [input, setInput] = useState("");
    const [isStreaming, setIsStreaming] = useState(false);
    const [streamingMessage, setStreamingMessage] = useState("");
    const [transcribedText, setTranscribedText] = useState("");
    const [isProcessingVoice, setIsProcessingVoice] = useState(false);
    const [isDrawerOpen, setIsDrawerOpen] = useState(false);
    const [suggestedStrategies, setSuggestedStrategies] = useState<
        SuggestedStrategy[]
    >([]);
    const [selectedStrategyIndexes, setSelectedStrategyIndexes] = useState<
        Set<number>
    >(new Set());
    const [isSavingStrategies, setIsSavingStrategies] = useState(false);
    const [answeredChoiceIndexes, setAnsweredChoiceIndexes] = useState<
        Set<number>
    >(new Set());
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const streamingMessageRef = useRef<string>("");
    const syncedConversationIdRef = useRef<string | undefined>(
        conversation?.id,
    );

    // Auto-scroll to bottom when messages change
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [messages, streamingMessage]);

    // Auto-resize textarea
    useEffect(() => {
        if (textareaRef.current) {
            textareaRef.current.style.height = "auto";
            textareaRef.current.style.height =
                textareaRef.current.scrollHeight + "px";
        }
    }, [input]);

    // Sync conversationId always; load messages only when navigating to a different thread.
    // Avoid overwriting local state after sends when router.refresh() returns stale server data.
    useEffect(() => {
        if (conversation) {
            setConversationId(conversation.id);
            if (syncedConversationIdRef.current !== conversation.id) {
                syncedConversationIdRef.current = conversation.id;
                setMessages(conversation.messages || []);
                setAnsweredChoiceIndexes(new Set());
            }
        } else {
            syncedConversationIdRef.current = undefined;
            setConversationId(undefined);
            setMessages([]);
            setAnsweredChoiceIndexes(new Set());
        }
    }, [conversation]);

    // In strategies mode, clear suggestions when switching threads (i.e. when the
    // server-provided `conversation` prop changes). We do NOT want to clear on
    // `conversationId` updates created by the current stream, otherwise we can
    // wipe suggestions right after receiving them.
    useEffect(() => {
        if (isStrategiesMode) {
            setSuggestedStrategies([]);
            setSelectedStrategyIndexes(new Set());
        }
    }, [isStrategiesMode, conversation?.id]);

    const processSseChunk = (
        buffer: string,
        chunkText: string,
        onParsed: (parsed: SseJsonMessage) => void,
    ) => {
        // Normalize CRLF to LF so delimiters are consistent
        let next = buffer + chunkText.replace(/\r\n/g, "\n");

        // SSE events are separated by a blank line (\n\n).
        // Each event can contain multiple `data:` lines.
        while (true) {
            const delimiterIndex = next.indexOf("\n\n");
            if (delimiterIndex < 0) break;

            const rawEvent = next.slice(0, delimiterIndex);
            next = next.slice(delimiterIndex + 2);

            const dataLines = rawEvent
                .split("\n")
                .filter((line) => line.startsWith("data:"));

            if (dataLines.length === 0) continue;

            const data = dataLines
                .map((line) => line.replace(/^data:\s?/, ""))
                .join("\n")
                .trim();

            if (!data) continue;

            try {
                onParsed(JSON.parse(data));
            } catch {
                // If parsing fails, ignore this event (rare once buffered).
            }
        }

        return next;
    };

    const flushSseBuffer = (
        buffer: string,
        onParsed: (parsed: SseJsonMessage) => void,
    ) => processSseChunk(buffer, "\n\n", onParsed);

    const normalizeSuggestedStrategies = (
        value: unknown,
    ): SuggestedStrategy[] => {
        if (!Array.isArray(value)) return [];

        const normalized = value.map((item) => {
            const obj =
                typeof item === "object" && item !== null
                    ? (item as Record<string, unknown>)
                    : {};

            const title =
                typeof obj.title === "string"
                    ? obj.title
                    : String(obj.title ?? "");
            const description =
                typeof obj.description === "string"
                    ? obj.description
                    : String(obj.description ?? "");

            const rawIconKey = obj.iconKey;
            const iconKey =
                rawIconKey === null
                    ? null
                    : typeof rawIconKey === "string"
                      ? rawIconKey === "null"
                          ? null
                          : rawIconKey
                      : null;

            const rawArticleUrl = obj.articleUrl;
            const articleUrl =
                rawArticleUrl === null
                    ? null
                    : typeof rawArticleUrl === "string"
                      ? rawArticleUrl === "null"
                          ? null
                          : rawArticleUrl
                      : null;

            return { title, description, iconKey, articleUrl };
        });
        return normalized;
    };

    const normalizeUserChoices = (value: unknown): UserChoicePrompt | null => {
        if (typeof value !== "object" || value === null) {
            return null;
        }

        const obj = value as Record<string, unknown>;
        const rawOptions = obj.options;
        if (!Array.isArray(rawOptions)) {
            return null;
        }

        const options = rawOptions
            .map((item) => {
                if (typeof item !== "object" || item === null) {
                    return null;
                }

                const option = item as Record<string, unknown>;
                const id =
                    typeof option.id === "string"
                        ? option.id
                        : String(option.id ?? "");
                const label =
                    typeof option.label === "string"
                        ? option.label
                        : String(option.label ?? "");

                if (!id || !label) {
                    return null;
                }

                return { id, label };
            })
            .filter(
                (option): option is { id: string; label: string } =>
                    option !== null,
            );

        if (options.length === 0) {
            return null;
        }

        return {
            prompt:
                typeof obj.prompt === "string" && obj.prompt.trim().length > 0
                    ? obj.prompt
                    : undefined,
            allowMultiple: obj.allowMultiple === true,
            options,
        };
    };

    const handleUserChoiceSelect = (messageIndex: number, label: string) => {
        if (isStreaming) return;
        setAnsweredChoiceIndexes((prev) => new Set(prev).add(messageIndex));
        void sendMessage(label);
    };

    const toggleStrategySelected = (index: number) => {
        setSelectedStrategyIndexes((prev) => {
            const next = new Set(prev);
            if (next.has(index)) {
                next.delete(index);
            } else {
                next.add(index);
            }
            return next;
        });
    };

    const handleSaveSelectedStrategies = async () => {
        if (selectedStrategyIndexes.size === 0) return;
        if (isSavingStrategies) return;

        try {
            setIsSavingStrategies(true);

            const selected = Array.from(selectedStrategyIndexes)
                .map((i) => suggestedStrategies[i])
                .filter(Boolean);

            await Promise.all(
                selected.map((s) =>
                    apiClient("/api/strategies", {
                        method: "POST",
                        body: {
                            title: s.title,
                            description: s.description,
                            iconKey: s.iconKey ?? null,
                        },
                    }),
                ),
            );

            await queryClient.invalidateQueries({
                queryKey: copingStrategyKeys.library(),
            });

            toast.success("Saved to your library");
            setSelectedStrategyIndexes(new Set());
        } catch (err) {
            toast.error(
                err instanceof Error
                    ? err.message
                    : "Failed to save strategies",
            );
        } finally {
            setIsSavingStrategies(false);
        }
    };

    const handleConfirmPendingAction = async (card: ActionCard) => {
        if (!card.workflowId || !card.pendingActionId) {
            toast.error("Missing confirmation details");
            return;
        }

        const response = await fetchWithAuth(
            `/api/agent/workflows/${card.workflowId}/actions/${card.pendingActionId}/confirm`,
            { method: "POST" },
        );

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || "Failed to confirm action");
        }

        const result = (await response.json()) as PendingActionConfirmResponse;
        if (!result.success) {
            throw new Error(result.message || "Failed to confirm action");
        }

        setMessages((prev) =>
            prev.map((message, index) => {
                if (index !== prev.length - 1 || message.role !== "assistant") {
                    return message;
                }

                const remainingCards = (message.actionCards ?? []).filter(
                    (c) =>
                        !(
                            c.cardType === "pending_confirmation" &&
                            c.pendingActionId === card.pendingActionId
                        ),
                );

                return {
                    ...message,
                    actionCards: [
                        ...remainingCards,
                        ...(result.actionCards ?? []),
                    ],
                    toolExecutions: result.toolExecution
                        ? [
                              ...(message.toolExecutions ?? []),
                              result.toolExecution,
                          ]
                        : message.toolExecutions,
                };
            }),
        );

        toast.success(result.message ?? "Action confirmed");
    };

    const handleCancelPendingAction = async (card: ActionCard) => {
        if (!card.workflowId || !card.pendingActionId) {
            toast.error("Missing confirmation details");
            return;
        }

        const response = await fetchWithAuth(
            `/api/agent/workflows/${card.workflowId}/actions/${card.pendingActionId}/cancel`,
            { method: "POST" },
        );

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || "Failed to cancel action");
        }

        setMessages((prev) =>
            prev.map((message, index) => {
                if (index !== prev.length - 1 || message.role !== "assistant") {
                    return message;
                }

                return {
                    ...message,
                    actionCards: (message.actionCards ?? []).filter(
                        (c) =>
                            !(
                                c.cardType === "pending_confirmation" &&
                                c.pendingActionId === card.pendingActionId
                            ),
                    ),
                };
            }),
        );

        toast.success("Action cancelled");
    };

    const handleVoiceRecording = async (audioBlob: Blob) => {
        if (isStreaming || isProcessingVoice) return;

        setIsProcessingVoice(true);
        setIsStreaming(true);
        setStreamingMessage("");
        setTranscribedText("");

        try {
            streamingMessageRef.current = "";

            // Create FormData for voice chat
            const formData = new FormData();
            formData.append("audio", audioBlob, "audio.wav");
            if (conversationId) {
                formData.append("conversationId", conversationId);
            }

            // Call API route for streaming voice chat
            const response = await fetchWithAuth("/api/chat/voice", {
                method: "POST",
                body: formData,
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || "Failed to stream voice chat");
            }

            // Get conversation ID from response header
            const newConversationId = response.headers.get("X-Conversation-Id");
            if (newConversationId) {
                setConversationId(newConversationId);
                if (
                    newConversationId &&
                    (!conversationId || newConversationId !== conversationId)
                ) {
                    router.push(
                        isStrategiesMode
                            ? `/app/user/chat/strategies/${newConversationId}`
                            : `/app/user/chat/${newConversationId}`,
                    );
                }
            }

            // Stream response using ReadableStream
            const reader = response.body?.getReader();
            const decoder = new TextDecoder();
            if (!reader) throw new Error("No response body");

            let citations: Citation[] = [];
            let sseBuffer = "";
            let done = false;
            const handleVoiceSseEvent = (parsed: SseJsonMessage) => {
                if (parsed.type === "citations" && parsed.value) {
                    citations = parsed.value as Citation[];
                    return;
                }

                if (parsed.type === "text" && parsed.value) {
                    streamingMessageRef.current += String(parsed.value);
                    setStreamingMessage(streamingMessageRef.current);
                    return;
                }

                if (
                    isStrategiesMode &&
                    parsed.type === "strategies" &&
                    Array.isArray(parsed.value)
                ) {
                    setSuggestedStrategies(
                        normalizeSuggestedStrategies(parsed.value),
                    );
                }
            };
            while (!done) {
                const { value, done: streamDone } = await reader.read();
                done = streamDone;
                if (value) {
                    const text = decoder.decode(value, { stream: true });
                    sseBuffer = processSseChunk(
                        sseBuffer,
                        text,
                        handleVoiceSseEvent,
                    );
                }
            }
            flushSseBuffer(sseBuffer, handleVoiceSseEvent);

            // Add complete assistant message
            if (streamingMessageRef.current) {
                const assistantMessage: ChatMessage = {
                    role: "assistant",
                    content: streamingMessageRef.current,
                    citations: citations.length > 0 ? citations : undefined,
                };
                setMessages((prev) => [...prev, assistantMessage]);
            }
        } catch (error) {
            console.error("Voice chat error:", error);
            // Show error message
            const errorMessage: ChatMessage = {
                role: "assistant",
                content:
                    "Sorry, I encountered an error processing your voice message. Please try again.",
            };
            setMessages((prev) => [...prev, errorMessage]);
        } finally {
            setIsProcessingVoice(false);
            setIsStreaming(false);
            setStreamingMessage("");
            setTranscribedText("");
        }
    };

    const sendMessage = async (messageContent: string) => {
        const trimmed = messageContent.trim();
        if (!trimmed || isStreaming) return;

        const userMessage: ChatMessage = {
            role: "user",
            content: trimmed,
        };

        if (isStrategiesMode) {
            setSuggestedStrategies([]);
            setSelectedStrategyIndexes(new Set());
        }

        // Add user message immediately
        setMessages((prev) => [...prev, userMessage]);
        setInput("");
        setIsStreaming(true);
        setStreamingMessage("");

        try {
            streamingMessageRef.current = "";

            // Check if agent feature is enabled
            if (agentFeatureEnabled) {
                // Use agent endpoint with tool execution support
                const requestBody: Record<string, unknown> = {
                    content: userMessage.content,
                };
                if (conversationId) {
                    requestBody.conversationId = conversationId;
                }
                if (workflowId) {
                    requestBody.workflowId = workflowId;
                }

                const response = await fetchWithAuth("/api/agent/stream", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify(requestBody),
                });

                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(errorText || "Failed to stream agent chat");
                }

                // Get conversation ID and workflow ID from response headers
                const newConversationId =
                    response.headers.get("X-Conversation-Id");
                if (newConversationId) {
                    setConversationId(newConversationId);
                    if (
                        isStrategiesMode &&
                        newConversationId !== conversationId
                    ) {
                        router.push(
                            `/app/user/chat/strategies/${newConversationId}`,
                        );
                    }
                }

                const newWorkflowIdHeader =
                    response.headers.get("X-Workflow-Id");
                if (newWorkflowIdHeader) {
                    setWorkflowId(newWorkflowIdHeader);
                }

                // Stream response using ReadableStream
                const reader = response.body?.getReader();
                const decoder = new TextDecoder();
                if (!reader) throw new Error("No response body");

                let citations: Citation[] = [];
                let toolExecutions: ToolExecution[] = [];
                let actionCards: ActionCard[] = [];
                let userChoices: UserChoicePrompt | null = null;
                let done = false;

                while (!done) {
                    const { value, done: streamDone } = await reader.read();
                    done = streamDone;
                    if (value) {
                        const text = decoder.decode(value, { stream: true });
                        // Assume SSE format: lines starting with 'data: '
                        const lines = text.split("\n");
                        for (const line of lines) {
                            if (line.startsWith("data: ")) {
                                const data = line.slice(6);
                                try {
                                    const parsed = JSON.parse(data);
                                    if (
                                        parsed.type === "citations" &&
                                        parsed.value
                                    ) {
                                        // Handle citations metadata
                                        citations = parsed.value;
                                    } else if (
                                        parsed.type === "text" &&
                                        parsed.value
                                    ) {
                                        // Handle text chunks
                                        streamingMessageRef.current +=
                                            parsed.value;
                                        setStreamingMessage(
                                            streamingMessageRef.current,
                                        );
                                    } else if (
                                        parsed.type === "tools_executed" &&
                                        parsed.value &&
                                        Array.isArray(parsed.value)
                                    ) {
                                        // Handle tool execution results
                                        toolExecutions =
                                            parsed.value as ToolExecution[];

                                        // Show toast notification if goals were created
                                        const goalsCreated =
                                            toolExecutions.some(
                                                (tool) =>
                                                    tool.toolName ===
                                                        "create_daily_goals" &&
                                                    tool.success,
                                            );
                                        if (goalsCreated) {
                                            toast.success(
                                                "Daily goals created successfully!",
                                                {
                                                    icon: "🎯",
                                                },
                                            );
                                        }
                                    } else if (
                                        parsed.type === "strategies" &&
                                        Array.isArray(parsed.value)
                                    ) {
                                        setSuggestedStrategies(
                                            parsed.value as SuggestedStrategy[],
                                        );
                                        setSelectedStrategyIndexes(new Set());
                                    } else if (
                                        parsed.type === "action_card" &&
                                        parsed.value
                                    ) {
                                        actionCards = [
                                            ...actionCards,
                                            parsed.value as ActionCard,
                                        ];
                                    } else if (
                                        parsed.type === "pending_action" &&
                                        parsed.value
                                    ) {
                                        actionCards = [
                                            ...actionCards,
                                            parsed.value as ActionCard,
                                        ];
                                    } else if (
                                        parsed.type === "user_choices" &&
                                        parsed.value
                                    ) {
                                        userChoices = normalizeUserChoices(
                                            parsed.value,
                                        );
                                    } else if (
                                        parsed.type === "workflow" &&
                                        parsed.value?.id
                                    ) {
                                        // Handle workflow ID
                                        setWorkflowId(parsed.value.id);
                                    } else if (
                                        parsed.type === "error" &&
                                        parsed.value
                                    ) {
                                        // Handle errors
                                        console.error(
                                            "Agent error:",
                                            parsed.value,
                                        );
                                        toast.error(
                                            `Agent error: ${parsed.value}`,
                                        );
                                    }
                                } catch {
                                    // Ignore parse errors
                                }
                            }
                        }
                    }
                }

                // Add complete assistant message with tool executions
                if (
                    streamingMessageRef.current ||
                    toolExecutions.length > 0 ||
                    actionCards.length > 0 ||
                    userChoices
                ) {
                    const assistantMessage: ChatMessage = {
                        role: "assistant",
                        content:
                            streamingMessageRef.current || "Action completed.",
                        citations: citations.length > 0 ? citations : undefined,
                        toolExecutions:
                            toolExecutions.length > 0
                                ? toolExecutions
                                : undefined,
                        actionCards:
                            actionCards.length > 0 ? actionCards : undefined,
                        userChoices: userChoices ?? undefined,
                        workflowId: newWorkflowIdHeader || workflowId,
                    };
                    setMessages((prev) => [...prev, assistantMessage]);
                }
            } else {
                // Use regular chat endpoint (existing behavior)
                const requestBody: Record<string, unknown> = {
                    content: userMessage.content,
                    conversationId,
                };
                if (isStrategiesMode) {
                    requestBody.mode = "strategies";
                }

                const response = await fetchWithAuth("/api/chat/stream", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify(requestBody),
                });

                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(errorText || "Failed to stream chat");
                }

                // Get conversation ID from response header
                const newConversationId =
                    response.headers.get("X-Conversation-Id");
                if (newConversationId) {
                    setConversationId(newConversationId);
                }

                // Stream response using ReadableStream
                const reader = response.body?.getReader();
                const decoder = new TextDecoder();
                if (!reader) throw new Error("No response body");

                let citations: Citation[] = [];
                let sseBuffer = "";
                let done = false;
                const handleChatSseEvent = (parsed: SseJsonMessage) => {
                    if (parsed.type === "citations" && parsed.value) {
                        citations = parsed.value as Citation[];
                        return;
                    }

                    if (parsed.type === "text" && parsed.value) {
                        streamingMessageRef.current += String(parsed.value);
                        setStreamingMessage(streamingMessageRef.current);
                        return;
                    }

                    if (
                        isStrategiesMode &&
                        parsed.type === "strategies" &&
                        Array.isArray(parsed.value)
                    ) {
                        console.log("parsed.value", parsed.value);
                        setSuggestedStrategies(
                            normalizeSuggestedStrategies(parsed.value),
                        );
                    }
                };
                while (!done) {
                    const { value, done: streamDone } = await reader.read();
                    done = streamDone;
                    if (value) {
                        const text = decoder.decode(value, { stream: true });
                        sseBuffer = processSseChunk(
                            sseBuffer,
                            text,
                            handleChatSseEvent,
                        );
                    }
                }
                flushSseBuffer(sseBuffer, handleChatSseEvent);

                // Add complete assistant message
                if (streamingMessageRef.current) {
                    const assistantMessage: ChatMessage = {
                        role: "assistant",
                        content: streamingMessageRef.current,
                        citations: citations.length > 0 ? citations : undefined,
                    };
                    setMessages((prev) => [...prev, assistantMessage]);
                }
            }
        } catch (error) {
            console.error("Chat error:", error);
            // Show error message
            const errorMessage: ChatMessage = {
                role: "assistant",
                content: "Sorry, I encountered an error. Please try again.",
            };
            setMessages((prev) => [...prev, errorMessage]);
        } finally {
            setIsStreaming(false);
            setStreamingMessage("");
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        await sendMessage(input);
    };

    const submitExamplePrompt = (prompt: string) => {
        if (isStreaming) return;
        setInput(prompt);
        // allow one render to show the populated input before sending
        setTimeout(() => {
            void sendMessage(prompt);
        }, 0);
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            handleSubmit(e as unknown as React.FormEvent);
        }
    };

    const handleConversationSelect = (conversationId: string) => {
        router.push(`/app/user/chat/${conversationId}`);
    };

    // Function to render message content with clickable citations
    const renderMessageWithCitations = (
        content: string,
        citations?: Citation[],
    ) => {
        if (!citations || citations.length === 0) {
            return content;
        }

        // Pattern to match citations like [1], [2], etc.
        const citationPattern = /\[(\d+)\]/g;
        const parts: (string | React.ReactNode)[] = [];
        let lastIndex = 0;
        let match;
        let keyCounter = 0;

        while ((match = citationPattern.exec(content)) !== null) {
            // Add text before the citation
            if (match.index > lastIndex) {
                parts.push(content.substring(lastIndex, match.index));
            }

            // Add the citation link
            const citationNum = parseInt(match[1], 10);
            const citation = citations.find((c) => c.index === citationNum);

            if (citation && citation.storageUrl) {
                parts.push(
                    <Link
                        key={`citation-${keyCounter++}`}
                        href={
                            citation.isShared
                                ? `/api/admin/resources/file/${citation.id}`
                                : `/api/user/resources/file/${citation.id}`
                        }
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-blue-600 hover:text-blue-800 underline font-medium"
                        title={`${citation.sourceFile}${
                            citation.sourcePage
                                ? ` - ${citation.sourcePage}`
                                : ""
                        }`}
                    >
                        [{citationNum}]
                    </Link>,
                );
            } else {
                // If no citation metadata, just render as text
                parts.push(`[${citationNum}]`);
            }

            lastIndex = match.index + match[0].length;
        }

        // Add remaining text
        if (lastIndex < content.length) {
            parts.push(content.substring(lastIndex));
        }

        return parts.length > 0 ? <>{parts}</> : content;
    };

    return (
        <div className="h-full flex flex-col relative">
            {/* Header with drawer toggle */}
            <div className="flex-shrink-0 bg-white border-b border-gray-200 px-4 py-2">
                <div className="flex items-center justify-between w-full">
                    <button
                        onClick={() => setIsDrawerOpen(true)}
                        className="p-2 text-gray-400 hover:text-gray-600 transition-colors"
                        aria-label="Open conversations"
                    >
                        <Bars3Icon className="h-6 w-6" />
                    </button>
                    <div className="flex-1 text-center">
                        <h1 className="text-lg font-semibold text-gray-900">
                            {conversation?.title || "Chat Assistant"}
                        </h1>
                    </div>
                    <div className="flex items-center justify-end w-44">
                        {isStrategiesMode ? (
                            <Link
                                href="/app/user/strategies"
                                className="text-sm font-medium text-indigo-700 hover:text-indigo-900"
                            >
                                Back to library
                            </Link>
                        ) : (
                            <div className="w-10" />
                        )}
                    </div>
                </div>
            </div>

            {/* Messages */}
            <div className="flex-1 overflow-y-auto px-4 py-4 min-h-0">
                <div className="w-full space-y-6">
                    {messages.length === 0 && (
                        <div className="text-center py-12">
                            <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-indigo-100 mb-4">
                                <svg
                                    className="w-8 h-8 text-indigo-600"
                                    fill="none"
                                    stroke="currentColor"
                                    viewBox="0 0 24 24"
                                >
                                    <path
                                        strokeLinecap="round"
                                        strokeLinejoin="round"
                                        strokeWidth={2}
                                        d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"
                                    />
                                </svg>
                            </div>
                            {isStrategiesMode ? (
                                <>
                                    <h3 className="text-lg font-medium text-gray-900 mb-2">
                                        Hi there {nameForGreeting}! 💚 I&apos;m
                                        here to help you find coping strategies
                                        that work for you. Just tell me what
                                        you&apos;re dealing with, and I&apos;ll
                                        suggest some techniques you might find
                                        helpful.
                                    </h3>
                                    <div className="mt-4 space-y-2">
                                        <p className="text-sm text-gray-500">
                                            Examples:
                                        </p>
                                        <div className="flex flex-col items-center gap-2">
                                            <button
                                                type="button"
                                                onClick={() =>
                                                    submitExamplePrompt(
                                                        "I'm feeling overwhelmed and overstimulated after work — what can I do right now?",
                                                    )
                                                }
                                                className="inline-flex max-w-full items-center rounded-full border border-gray-300 bg-white px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                                            >
                                                I&apos;m feeling overwhelmed and
                                                overstimulated after work — what
                                                can I do right now?
                                            </button>
                                            <button
                                                type="button"
                                                onClick={() =>
                                                    submitExamplePrompt(
                                                        "I'm anxious about a social event later — can you give me a few coping strategies I can try beforehand?",
                                                    )
                                                }
                                                className="inline-flex max-w-full items-center rounded-full border border-gray-300 bg-white px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                                            >
                                                I&apos;m anxious about a social
                                                event later — what can I try
                                                beforehand?
                                            </button>
                                            <button
                                                type="button"
                                                onClick={() =>
                                                    submitExamplePrompt(
                                                        "My brain won't switch off and I'm stuck in a spiral — what can I do in the next 5 minutes?",
                                                    )
                                                }
                                                className="inline-flex max-w-full items-center rounded-full border border-gray-300 bg-white px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                                            >
                                                My brain won&apos;t switch off —
                                                what can I do in the next 5
                                                minutes?
                                            </button>
                                        </div>
                                    </div>
                                </>
                            ) : (
                                <>
                                    <h3 className="text-lg font-medium text-gray-900 mb-2">
                                        Start a conversation
                                    </h3>
                                    <p className="text-sm text-gray-500">
                                        Ask me anything! I&apos;m here to help.
                                    </p>
                                </>
                            )}
                        </div>
                    )}

                    {messages.map((message, index) => (
                        <div
                            key={index}
                            className={`flex ${
                                message.role === "user"
                                    ? "justify-end"
                                    : "justify-start"
                            }`}
                        >
                            <div className="max-w-4xl">
                                <div
                                    className={`rounded-lg px-4 py-3 ${
                                        message.role === "user"
                                            ? "bg-indigo-600 text-white"
                                            : "bg-white border border-gray-200 text-gray-900"
                                    }`}
                                >
                                    <div className="text-xs font-medium mb-1 opacity-75">
                                        {message.role === "user"
                                            ? "You"
                                            : "Assistant"}
                                    </div>
                                    <div className="text-sm whitespace-pre-wrap">
                                        {message.role === "assistant" &&
                                        message.citations
                                            ? renderMessageWithCitations(
                                                  message.content,
                                                  message.citations,
                                              )
                                            : message.content}
                                    </div>
                                </div>
                                {/* Tool execution badges (only for assistant messages and if feature flag enabled) */}
                                {agentFeatureEnabled &&
                                    message.role === "assistant" &&
                                    message.toolExecutions &&
                                    message.toolExecutions.filter(
                                        (tool) => tool.toolName !== "ask_user",
                                    ).length > 0 && (
                                        <div className="mt-2 space-y-2">
                                            {message.toolExecutions
                                                .filter(
                                                    (tool) =>
                                                        tool.toolName !==
                                                        "ask_user",
                                                )
                                                .map((tool, toolIndex) => (
                                                    <ToolExecutionBadge
                                                        key={toolIndex}
                                                        tool={tool}
                                                    />
                                                ))}
                                        </div>
                                    )}
                                {agentFeatureEnabled &&
                                    message.role === "assistant" &&
                                    message.actionCards &&
                                    message.actionCards.length > 0 && (
                                        <div className="mt-2 space-y-2">
                                            {message.actionCards.map(
                                                (card, cardIndex) => (
                                                    <ActionCardWidget
                                                        key={cardIndex}
                                                        card={card}
                                                        onConfirmPendingAction={
                                                            handleConfirmPendingAction
                                                        }
                                                        onCancelPendingAction={
                                                            handleCancelPendingAction
                                                        }
                                                    />
                                                ),
                                            )}
                                        </div>
                                    )}
                                {agentFeatureEnabled &&
                                    message.role === "assistant" &&
                                    message.userChoices &&
                                    !answeredChoiceIndexes.has(index) && (
                                        <UserChoiceChips
                                            choices={message.userChoices}
                                            disabled={isStreaming}
                                            onSelect={(label) =>
                                                handleUserChoiceSelect(
                                                    index,
                                                    label,
                                                )
                                            }
                                        />
                                    )}
                            </div>
                        </div>
                    ))}

                    {/* Transcribed text preview (during voice processing) */}
                    {isProcessingVoice && transcribedText && (
                        <div className="flex justify-end">
                            <div className="max-w-4xl rounded-lg px-4 py-3 bg-indigo-100 border border-indigo-200 text-indigo-900">
                                <div className="text-xs font-medium mb-1 opacity-75">
                                    Transcribed from voice
                                </div>
                                <div className="text-sm whitespace-pre-wrap">
                                    {transcribedText}
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Streaming message */}
                    {isStreaming && streamingMessage && (
                        <div className="flex justify-start">
                            <div className="max-w-4xl rounded-lg px-4 py-3 bg-white border border-gray-200 text-gray-900">
                                <div className="text-xs font-medium mb-1 opacity-75">
                                    Assistant
                                </div>
                                <div className="text-sm whitespace-pre-wrap">
                                    {streamingMessage}
                                    <span className="inline-block w-2 h-4 ml-1 bg-gray-400 animate-pulse" />
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Strategy suggestions (strategies mode only) */}
                    {isStrategiesMode && suggestedStrategies.length > 0 && (
                        <div className="space-y-3">
                            <div className="flex items-center justify-between">
                                <h3 className="text-sm font-semibold text-gray-900">
                                    Suggested strategies
                                </h3>
                                {selectedStrategyIndexes.size > 0 && (
                                    <button
                                        type="button"
                                        onClick={handleSaveSelectedStrategies}
                                        disabled={isSavingStrategies}
                                        className="inline-flex items-center rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
                                    >
                                        {isSavingStrategies
                                            ? "Saving..."
                                            : `Save (${selectedStrategyIndexes.size})`}
                                    </button>
                                )}
                            </div>
                            <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
                                {suggestedStrategies.slice(0, 3).map((s, i) => {
                                    const selected =
                                        selectedStrategyIndexes.has(i);
                                    return (
                                        <div
                                            key={`${s.title}-${i}`}
                                            className={`text-left rounded-lg border p-4 transition-colors ${
                                                selected
                                                    ? "border-indigo-500 bg-indigo-50"
                                                    : "border-gray-200 bg-white hover:bg-gray-50"
                                            }`}
                                        >
                                            <button
                                                type="button"
                                                onClick={() =>
                                                    toggleStrategySelected(i)
                                                }
                                                className="w-full text-left"
                                            >
                                                <p className="text-sm font-semibold text-gray-900">
                                                    {s.title}
                                                </p>
                                                <p className="mt-1 text-sm text-gray-600">
                                                    {s.description}
                                                </p>
                                            </button>

                                            {s.articleUrl &&
                                                isSafeExternalUrl(
                                                    s.articleUrl,
                                                ) && (
                                                    <div className="mt-3">
                                                        <a
                                                            href={s.articleUrl}
                                                            target="_blank"
                                                            rel="noopener noreferrer"
                                                            className="text-xs font-medium text-indigo-700 hover:text-indigo-900 underline"
                                                        >
                                                            Learn more
                                                        </a>
                                                    </div>
                                                )}
                                        </div>
                                    );
                                })}
                            </div>
                        </div>
                    )}

                    {/* Loading indicator */}
                    {isStreaming && !streamingMessage && (
                        <div className="flex justify-start">
                            <div className="max-w-4xl rounded-lg px-4 py-3 bg-white border border-gray-200">
                                <div className="text-xs font-medium mb-1 opacity-75">
                                    {isProcessingVoice
                                        ? "Processing voice..."
                                        : "Assistant"}
                                </div>
                                <div className="flex space-x-2">
                                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" />
                                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce delay-75" />
                                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce delay-150" />
                                </div>
                            </div>
                        </div>
                    )}

                    <div ref={messagesEndRef} />
                </div>
            </div>

            {/* Input */}
            <div className="flex-shrink-0 bg-white border-t border-gray-200 px-4 py-3">
                <form onSubmit={handleSubmit} className="w-full">
                    <div className="flex items-end space-x-3">
                        <div className="flex-1">
                            <textarea
                                ref={textareaRef}
                                value={input}
                                onChange={(e) => setInput(e.target.value)}
                                onKeyDown={handleKeyDown}
                                placeholder={
                                    isStrategiesMode
                                        ? "Tell me what you're dealing with... (Press Enter to send)"
                                        : "Type or speak your message... (Press Enter to send, Shift+Enter for new line)"
                                }
                                disabled={isStreaming}
                                rows={1}
                                className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-none disabled:bg-gray-100 disabled:cursor-not-allowed"
                                style={{ maxHeight: "200px" }}
                            />
                        </div>
                        <FeatureGate
                            feature="speech_input"
                            showUpgradePrompt={false}
                        >
                            <VoiceRecorder
                                onRecordingComplete={handleVoiceRecording}
                                onError={(error) =>
                                    console.error(
                                        "Voice recording error:",
                                        error,
                                    )
                                }
                                disabled={isStreaming}
                            />
                        </FeatureGate>
                        <button
                            type="submit"
                            disabled={!input.trim() || isStreaming}
                            className="flex-shrink-0 inline-flex items-center justify-center w-10 h-10 rounded-lg bg-indigo-600 text-white hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                        >
                            <PaperAirplaneIcon className="h-5 w-5" />
                        </button>
                    </div>
                    <p className="mt-2 text-xs text-gray-500">
                        Press Enter to send, Shift+Enter for a new line, or
                        click the microphone to record voice
                    </p>
                </form>
            </div>

            {/* Conversations Drawer */}
            <ConversationsDrawer
                isOpen={isDrawerOpen}
                onClose={() => setIsDrawerOpen(false)}
                currentConversationId={conversationId}
                onConversationSelect={handleConversationSelect}
            />
        </div>
    );
}
