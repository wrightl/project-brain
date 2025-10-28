import { auth0 } from '@/lib/auth';
import { NextResponse } from 'next/server';

export const dynamic = 'force-dynamic';

/**
 * API route to get the access token for authenticated users
 * This allows client components to get the token without directly accessing cookies
 */
export async function GET() {
  try {
    const session = await auth0.getSession();

    console.log('Session exists:', !!session);
    console.log('Session user:', session?.user?.email);
    console.log('Access token exists:', !!session?.accessToken);

    if (!session || !session.accessToken) {
      console.error('No session or access token found');
      return NextResponse.json(
        { error: 'No access token available' },
        { status: 401 }
      );
    }

    return NextResponse.json({ accessToken: session.accessToken });
  } catch (error) {
    console.error('Error getting access token:', error);
    return NextResponse.json(
      { error: 'Failed to get access token' },
      { status: 500 }
    );
  }
}
