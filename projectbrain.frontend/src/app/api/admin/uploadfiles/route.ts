import { createApiRoute } from '@/_lib/api-route-handler';
import { getAccessToken } from '@/_lib/auth';
import { NextRequest, NextResponse } from 'next/server';

export const POST = createApiRoute(async (req: NextRequest) => {
    const fileData = await req.formData();

    // Loop through all received files and append them.
    // Use the same key 'file' to send an array of files.
    const formData = new FormData();
    const files = fileData.getAll('file');
    for (const file of files) {
        formData.append('file', file);
    }

    const API_URL = process.env.API_SERVER_URL || 'https://localhost:7585';
    const accessToken = await getAccessToken();

    try {
        const data = await fetch(`${API_URL}/resource/upload/shared`, {
            method: 'POST',
            body: formData,
            headers: {
                Authorization: `Bearer ${accessToken}`,
            },
        });

        if (!data.ok) {
            // Try to parse error response
            let errorMessage = 'Upload failed';
            let errorType: 'network' | 'limit' | 'http' = 'http';

            try {
                const errorData = await data.json();
                errorMessage = errorData.error || errorMessage;
                
                // Check if it's a limit error
                if (data.status === 400 && (
                    errorMessage.toLowerCase().includes('limit') ||
                    errorMessage.toLowerCase().includes('exceeded')
                )) {
                    errorType = 'limit';
                }
            } catch {
                // If JSON parsing fails, try text
                try {
                    errorMessage = await data.text() || errorMessage;
                } catch {
                    // Use default error message
                }
            }

            return NextResponse.json(
                {
                    status: 'error',
                    error: errorMessage,
                    errorType,
                },
                { status: data.status }
            );
        }

        return NextResponse.json(await data.json(), { status: 201 });
    } catch (error) {
        // Network error or fetch failure
        const errorMessage =
            error instanceof Error ? error.message : 'Network error occurred';
        return NextResponse.json(
            {
                status: 'error',
                error: errorMessage,
                errorType: 'network' as const,
            },
            { status: 500 }
        );
    }
});
