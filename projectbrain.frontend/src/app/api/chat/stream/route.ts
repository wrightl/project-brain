import { getAccessToken } from '@/_lib/auth';
import { NextRequest } from 'next/server';

export async function POST(req: NextRequest) {
    const { content, conversationId } = await req.json();

    const accessToken = await getAccessToken();

    // Prepare headers
    const headers: HeadersInit = {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
    };

    // Use native fetch for streaming
    const backendResponse = await fetch(
        `${process.env.API_SERVER_URL}/chat/stream`,
        {
            method: 'POST',
            headers,
            body: JSON.stringify({ content, conversationId }),
        }
    );

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
}
