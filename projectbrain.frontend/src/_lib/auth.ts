import { UserRole } from '@/_lib/types';
import { Auth0Client } from '@auth0/nextjs-auth0/server';
import { GetAccessTokenOptions } from '@auth0/nextjs-auth0/types';
import { NextRequest, NextResponse } from 'next/server';

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
    const session = await auth0.getSession();
    if (!session?.user) return null;

    // Auth0 custom claims must be namespaced
    // Role should be set in Auth0 rules/actions as app_metadata
    const role = session.user['https://projectbrain.app/role'] as
        | UserRole[]
        | undefined;

    // Default to 'user' role if not specified
    return role || ['user'];
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

// /**
//  * Get access token for API calls
//  * Falls back to ID token if access token is not available
//  */
// export async function getAccessToken(): Promise<string | null> {
//     const session = await auth0.getSession();

//     console.log('Session exists:', !!session);
//     console.log('Session user:', session?.user?.email);
//     console.log('Access token exists:', !!session?.accessToken);
//     console.log('ID token exists:', !!session?.idToken);

//     // Try to get access token first (for API authorization)
//     let token = session?.accessToken;

//     // Fallback to ID token if access token is not available
//     // This can happen if Auth0 audience is not properly configured
//     if (!token) {
//         console.warn('Access token not found, falling back to ID token');
//         token = session?.idToken;
//     }

//     return typeof token === 'string' ? token : null;
// }

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
    const tokenResponse = await auth0.getAccessToken(options);

    // Handle the case where tokenResponse might be undefined
    if (!tokenResponse || !tokenResponse.token) {
        throw new Error('Unable to retrieve access token');
    }

    return tokenResponse.token;
}

export async function authMiddleware(req: NextRequest): Promise<NextResponse> {
    return await auth0.middleware(req);
}
