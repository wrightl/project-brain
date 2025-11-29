import { NextRequest, NextResponse } from 'next/server';
import https from 'https';
import { UserOnboardingData } from '@/_lib/types';
import { getAccessToken } from '@/_lib/auth';

export const dynamic = 'force-dynamic';

// Create a custom agent that accepts self-signed certificates in development
const httpsAgent =
    process.env.NODE_ENV === 'development'
        ? new https.Agent({
              rejectUnauthorized: false, // Allow self-signed certificates in dev
          })
        : undefined;

/**
 * API route to handle user onboarding
 * This server-side route proxies the request to the backend API with proper authentication
 */
export async function POST(request: NextRequest) {
    try {
        // Get the session and access token
        const token = await getAccessToken();

        // if (!tokenResponse) {
        //     console.error('No session or access token found');
        //     return NextResponse.json(
        //         { error: 'Unauthorized - No access token available' },
        //         { status: 401 }
        //     );
        // }

        // Get the API server URL from environment variables (server-side only)
        const apiServerUrl =
            process.env.API_SERVER_URL || 'https://localhost:7585';

        if (!apiServerUrl) {
            console.error('API_SERVER_URL is not configured');
            return NextResponse.json(
                { error: 'Server configuration error' },
                { status: 500 }
            );
        }

        // Parse the request body
        const body: UserOnboardingData = await request.json();

        // Make the request to the backend API
        const response = await fetch(
            `${apiServerUrl}/users/me/onboarding/coach`,
            {
                method: 'POST',
                headers: {
                    Authorization: `Bearer ${token}`,
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(body),
                // @ts-expect-error - agent is valid but TypeScript doesn't recognize it in fetch
                agent: httpsAgent,
            }
        );

        // Handle the response
        if (!response.ok) {
            const errorMessage = `API Error (${response.status}, token: ${token}, url: ${apiServerUrl})`;

            // try {
            //     const contentType = response.headers.get('content-type');
            //     if (contentType?.includes('application/json')) {
            //         const errorData = await response.json();
            //         errorMessage =
            //             errorData.message || errorData.error || errorMessage;
            //     } else {
            //         const errorText = await response.text();
            //         if (errorText)
            //             errorMessage = `${errorMessage}: ${errorText}`;
            //     }
            // } catch {
            //     // Use default message if parsing fails
            // }

            console.error('Backend API error:', errorMessage);
            return NextResponse.json(
                { error: errorMessage },
                { status: response.status }
            );
        }

        // Return the successful response
        const userData = await response.json();
        return NextResponse.json(userData);
    } catch (error) {
        console.error('Error in onboarding API route:', error);
        return NextResponse.json(
            {
                error:
                    error instanceof Error
                        ? error.message
                        : 'Failed to complete onboarding',
            },
            { status: 500 }
        );
    }
}
