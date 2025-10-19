import { getUserRole } from '@/lib/auth';
import { redirect } from 'next/navigation';
import AdminDashboard from '@/components/dashboard/admin-dashboard';
import CoachDashboard from '@/components/dashboard/coach-dashboard';
import UserDashboard from '@/components/dashboard/user-dashboard';

export default async function DashboardPage() {
  const role = await getUserRole();

  // Redirect based on role
  if (!role) {
    redirect('/api/auth/login');
  }

  return (
    <div>
      {role === 'admin' && <AdminDashboard />}
      {role === 'coach' && <CoachDashboard />}
      {role === 'user' && <UserDashboard />}
    </div>
  );
}
