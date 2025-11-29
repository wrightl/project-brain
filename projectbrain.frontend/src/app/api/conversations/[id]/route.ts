import { NextRequest, NextResponse } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { ConversationService } from '@/_services/conversation-service';

export const DELETE = createApiRoute(async (req: NextRequest) => {
    try {
        // Extract the ID from the URL pathname
        // URL format: /api/conversations/[id]
        const pathname = req.nextUrl.pathname;
        const id = pathname.split('/').pop();

        if (!id) {
            return NextResponse.json(
                { error: 'Conversation ID is required' },
                { status: 400 }
            );
        }

        await ConversationService.deleteConversation(id);

        return NextResponse.json({ success: true });
    } catch (error) {
        console.error('Delete conversation error:', error);
        return NextResponse.json(
            { error: 'Internal server error' },
            { status: 500 }
        );
    }
});

