import { getUserRole } from '@/lib/auth';
import { redirect } from 'next/navigation';

/**
 * Main dashboard route - redirects users to their role-specific dashboard
 */
export default async function DashboardPage() {
  const role = await getUserRole();

  // Redirect based on user role
  if (!role) {
    redirect('/api/auth/login');
  }

  switch (role) {
    case 'admin':
      redirect('/admin');
    case 'coach':
      redirect('/coach');
    case 'user':
      redirect('/user');
    default:
      redirect('/api/auth/login');
  }
}
