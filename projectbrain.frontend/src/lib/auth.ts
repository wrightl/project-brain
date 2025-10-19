import { getSession } from '@auth0/nextjs-auth0';
import { UserRole } from '@/types/user';

/**
 * Get user role from Auth0 session
 * Roles should be added to the user's app_metadata in Auth0
 */
export async function getUserRole(): Promise<UserRole | null> {
  const session = await getSession();
  if (!session?.user) return null;

  // Auth0 custom claims must be namespaced
  // Role should be set in Auth0 rules/actions as app_metadata
  const role = session.user['https://projectbrain.app/role'] as UserRole | undefined;

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
  const session = await getSession();
  return session?.accessToken || null;
}
