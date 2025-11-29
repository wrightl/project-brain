import { createApiRoute } from '@/_lib/api-route-handler';
import { ConversationService } from '@/_services/conversation-service';
import { Conversation } from '@/_lib/types';
import { NextRequest } from 'next/server';

export const POST = createApiRoute<Conversation>(async (req: NextRequest) => {
    const body = await req.json();
    const { title } = body;

    if (!title || typeof title !== 'string') {
        throw new Error('Title is required');
    }

    const conversation = await ConversationService.createConversation(title);
    return conversation;
});

export const GET = createApiRoute<Conversation[]>(async (req: NextRequest) => {
    const conversations = await ConversationService.getConversations();
    return conversations;
});
