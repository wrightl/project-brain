import { createApiRoute } from '@/_lib/api-route-handler';
import { ConversationService } from '@/_services/conversation-service';
import { Conversation, PagedResponse } from '@/_lib/types';
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

export const GET = createApiRoute<PagedResponse<Conversation>>(async (req: NextRequest) => {
    const { searchParams } = new URL(req.url);
    const pageParam = searchParams.get('page');
    const pageSizeParam = searchParams.get('pageSize');

    const options = {
        page: pageParam ? parseInt(pageParam, 10) : undefined,
        pageSize: pageSizeParam ? parseInt(pageSizeParam, 10) : undefined,
    };

    const result = await ConversationService.getConversations(options);
    return result;
});
