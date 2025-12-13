import { getAccessToken } from '@/_lib/auth';
import { NextResponse } from 'next/server';

export const dynamic = 'force-dynamic';

/**
 * API route to get the access token for authenticated users
 * This allows client components to get the token for SignalR connections
 */
export async function GET() {
    try {
        const accessToken = await getAccessToken();

        if (!accessToken) {
            return NextResponse.json(
                { error: 'No access token available' },
                { status: 401 }
            );
        }

        return NextResponse.json({ token: accessToken });
    } catch (error) {
        console.error('Error getting access token:', error);
        return NextResponse.json(
            { error: 'Failed to get access token' },
            { status: 500 }
        );
    }
}

