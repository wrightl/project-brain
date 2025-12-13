import { getAccessToken } from '@/_lib/auth';
import { NextRequest, NextResponse } from 'next/server';

export async function GET(
    req: NextRequest,
    { params }: { params: { messageId: string } }
) {
    try {
        const { messageId } = params;
        const accessToken = await getAccessToken();
        const apiUrl = process.env.API_SERVER_URL || 'https://localhost:7585';

        const response = await fetch(`${apiUrl}/coach-messages/${messageId}/audio`, {
            headers: {
                Authorization: `Bearer ${accessToken}`,
            },
        });

        if (!response.ok) {
            return NextResponse.json(
                { error: 'Failed to fetch audio' },
                { status: response.status }
            );
        }

        const audioBlob = await response.blob();
        const contentType = response.headers.get('Content-Type') || 'audio/m4a';

        return new NextResponse(audioBlob, {
            headers: {
                'Content-Type': contentType,
            },
        });
    } catch (error) {
        console.error('Error fetching audio:', error);
        return NextResponse.json(
            { error: 'Failed to fetch audio' },
            { status: 500 }
        );
    }
}

