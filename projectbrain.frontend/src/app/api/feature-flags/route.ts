import { getAccessToken } from '@/_lib/auth';
import { NextRequest, NextResponse } from 'next/server';

export async function GET(req: NextRequest) {
    try {
        const accessToken = await getAccessToken();

        // Get the API server URL from environment variables
        const apiServerUrl =
            process.env.API_SERVER_URL || 'https://localhost:7585';

        if (!apiServerUrl) {
            console.error('API_SERVER_URL is not configured');
            return NextResponse.json(
                { error: 'Server configuration error' },
                { status: 500 }
            );
        }

        // Make the request to the backend API
        const response = await fetch(`${apiServerUrl}/feature-flags`, {
            method: 'GET',
            headers: {
                Authorization: `Bearer ${accessToken}`,
                'Content-Type': 'application/json',
            },
        });

        if (!response.ok) {
            return NextResponse.json(
                { error: `Backend API error: ${response.status} ${response.statusText}` },
                { status: response.status }
            );
        }

        const flags = await response.json();

        return NextResponse.json(flags);
    } catch (error) {
        console.error('Error fetching feature flags:', error);
        return NextResponse.json(
            { error: 'Failed to fetch feature flags' },
            { status: 500 }
        );
    }
}

