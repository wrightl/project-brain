import { redirect } from 'next/navigation';
import { getUserRole } from '@/lib/auth';
import { UserRole } from '@/types/user';

interface RoleGuardProps {
  children: React.ReactNode;
  allowedRoles: UserRole[];
  redirectTo?: string;
}

/**
 * Server Component that guards routes based on user role
 */
export async function RoleGuard({
  children,
  allowedRoles,
  redirectTo = '/dashboard',
}: RoleGuardProps) {
  const userRole = await getUserRole();

  if (!userRole || !allowedRoles.includes(userRole)) {
    redirect(redirectTo);
  }

  return <>{children}</>;
}
