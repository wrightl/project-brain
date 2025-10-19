import { Auth0Client } from '@auth0/nextjs-auth0/server';
import { UserRole } from '@/types/user';

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
 * Roles should be added to the user's app_metadata in Auth0
 */
export async function getUserRole(): Promise<UserRole | null> {
    const session = await auth0.getSession();
    if (!session?.user) return null;

    // Auth0 custom claims must be namespaced
    // Role should be set in Auth0 rules/actions as app_metadata
    const role = session.user['https://projectbrain.app/role'] as
        | UserRole
        | undefined;

    // Default to 'user' role if not specified
    return role || 'user';
}

/**
 * Check if user has required role
 */
export async function hasRole(requiredRole: UserRole): Promise<boolean> {
    const userRole = await getUserRole();
    if (!userRole) return false;

    const roleHierarchy: Record<UserRole, number> = {
        user: 1,
        coach: 2,
        admin: 3,
    };

    return roleHierarchy[userRole] >= roleHierarchy[requiredRole];
}

/**
 * Get access token for API calls
 */
export async function getAccessToken(): Promise<string | null> {
    const session = await auth0.getSession();
    const token = session?.accessToken;
    return typeof token === 'string' ? token : null;
}
