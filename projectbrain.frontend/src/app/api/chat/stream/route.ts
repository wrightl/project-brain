import { createStreamingApiRoute } from '@/_lib/api-route-handler';
import { getAccessToken } from '@/_lib/auth';
import { NextRequest } from 'next/server';

export const POST = createStreamingApiRoute(async (req: NextRequest) => {
    const { content, conversationId, mode } = await req.json();
    const accessToken = await getAccessToken();

    const headers: HeadersInit = {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
    };

    const backendResponse = await fetch(
        `${process.env.API_SERVER_URL}/chat/stream`,
        {
            method: 'POST',
            headers,
            body: JSON.stringify({ content, conversationId, mode }),
        }
    );

    if (!backendResponse.ok) {
        const errorText = await backendResponse.text();
        return new Response(errorText || 'Stream request failed', {
            status: backendResponse.status,
            headers: { 'Content-Type': 'application/json' },
        });
    }

    const stream = backendResponse.body;
    const conversationIdHeader =
        backendResponse.headers.get('X-Conversation-Id');

    return new Response(stream, {
        status: backendResponse.status,
        headers: {
            'Content-Type': 'text/event-stream',
            ...(conversationIdHeader
                ? { 'X-Conversation-Id': conversationIdHeader }
                : {}),
        },
    });
});
