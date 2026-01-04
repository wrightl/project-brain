import { UserRole } from '@/_lib/types';
import { Auth0Client } from '@auth0/nextjs-auth0/server';
import { GetAccessTokenOptions } from '@auth0/nextjs-auth0/types';
import { NextRequest, NextResponse } from 'next/server';
import { redirect } from 'next/navigation';
import jwt, { JwtPayload } from 'jsonwebtoken';

export const dynamic = 'force-dynamic';

// Initialize the Auth0 client
export const auth0 = new Auth0Client({
    // Options are loaded from environment variables by default
    // Ensure necessary environment variables are properly set
    // domain: process.env.AUTH0_DOMAIN,
    // clientId: process.env.AUTH0_CLIENT_ID,
    // clientSecret: process.env.AUTH0_CLIENT_SECRET,
    // appBaseUrl: process.env.APP_BASE_URL,
    // secret: process.env.AUTH0_SECRET,

    authorizationParameters: {
        // In v4, the AUTH0_SCOPE and AUTH0_AUDIENCE environment variables for API authorized applications are no longer automatically picked up by the SDK.
        // Instead, we need to provide the values explicitly.
        scope: process.env.AUTH0_SCOPE,
        audience: process.env.AUTH0_AUDIENCE,
    },
});

/**
 * Get user role from Auth0 session
 *R oles should be added to the user's app_metadata in Auth0
 */
export async function getUserRoles(): Promise<UserRole[] | null> {
    const accessToken = await getAccessToken();
    if (!accessToken) return null;

    const decoded = jwt.decode(accessToken) as JwtPayload;
    console.log('decoded', decoded);
    return decoded['https://projectbrain.app/roles'] || null;
}

/**
 * Check if user has required role
 */
export async function hasRole(requiredRole: UserRole): Promise<boolean> {
    const userRole = await getUserRoles();
    if (!userRole) return false;

    const roleHierarchy: Record<UserRole, number> = {
        user: 1,
        coach: 2,
        admin: 3,
    };

    return roleHierarchy[userRole[0]] >= roleHierarchy[requiredRole];
}

/**
 * Get user email from Auth0 session
 */
export async function getUserEmail(): Promise<string | null> {
    const session = await auth0.getSession();
    return session?.user?.email || null;
}

export async function getAccessToken(
    options?: GetAccessTokenOptions
): Promise<string> {
    try {
        const tokenResponse = await auth0.getAccessToken(options);

        // Handle the case where tokenResponse might be undefined
        if (!tokenResponse || !tokenResponse.token) {
            redirect('/auth/login?returnTo=/app');
        }

        return tokenResponse.token;
    } catch (error) {
        // If it's a NEXT_REDIRECT error, re-throw it to allow the redirect to propagate
        if (
            typeof error === 'object' &&
            error !== null &&
            'message' in error &&
            (error as { message: string }).message === 'NEXT_REDIRECT'
        ) {
            throw error;
        }
        // If we can't get a token, the session is likely expired - redirect to login
        redirect('/auth/login?returnTo=/app');
    }
}

export async function authMiddleware(req: NextRequest): Promise<NextResponse> {
    return await auth0.middleware(req);
}

export async function getSession() {
    return await auth0.getSession();
}
