import { NextRequest, NextResponse } from 'next/server';

export async function GET(req: NextRequest) {
    try {
        const token = req.nextUrl.searchParams.get('token');
        if (!token) {
            return NextResponse.json(
                { error: 'Token is required' },
                { status: 400 }
            );
        }

        const apiServerUrl =
            process.env.API_SERVER_URL || 'https://localhost:7585';

        const response = await fetch(
            `${apiServerUrl}/referrals/preview?token=${encodeURIComponent(token)}`,
            {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                },
                cache: 'no-store',
            }
        );

        if (!response.ok) {
            const payload = await response.json().catch(() => null);
            return NextResponse.json(
                payload || {
                    error: `Backend API error: ${response.status} ${response.statusText}`,
                },
                { status: response.status }
            );
        }

        const preview = await response.json();
        return NextResponse.json(preview);
    } catch (error) {
        console.error('Error previewing referral invite:', error);
        return NextResponse.json(
            { error: 'Failed to preview referral invite' },
            { status: 500 }
        );
    }
}

