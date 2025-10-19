import { RoleGuard } from '@/components/auth/role-guard';
import { getUserRole } from '@/lib/auth';
import DashboardNav from '@/components/dashboard/dashboard-nav';
import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Coach',
  description: 'Coach dashboard and tools',
};

export default async function CoachLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const role = await getUserRole();

  return (
    <RoleGuard allowedRoles={['coach']} redirectTo="/dashboard">
      <div className="min-h-screen bg-gray-50">
        <DashboardNav user={null} role={role} />
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          {children}
        </main>
      </div>
    </RoleGuard>
  );
}
