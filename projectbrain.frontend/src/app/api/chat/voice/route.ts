import { getAccessToken } from '@/_lib/auth';
import { NextRequest } from 'next/server';

export async function POST(req: NextRequest) {
    const formData = await req.formData();
    const audioFile = formData.get('audio') as File;
    const conversationId = formData.get('conversationId') as string | null;

    if (!audioFile) {
        return new Response('No audio file provided', { status: 400 });
    }

    const accessToken = await getAccessToken();

    // Create FormData for backend request
    const backendFormData = new FormData();
    backendFormData.append('audio', audioFile);
    if (conversationId) {
        backendFormData.append('conversationId', conversationId);
    }

    // Prepare headers
    const headers: HeadersInit = {
        Authorization: `Bearer ${accessToken}`,
    };

    // Use native fetch for streaming
    const backendResponse = await fetch(
        `${process.env.API_SERVER_URL}/chat/voice`,
        {
            method: 'POST',
            headers,
            body: backendFormData,
        }
    );

    if (!backendResponse.ok) {
        const errorText = await backendResponse.text();
        return new Response(errorText || 'Voice chat failed', {
            status: backendResponse.status,
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
}
