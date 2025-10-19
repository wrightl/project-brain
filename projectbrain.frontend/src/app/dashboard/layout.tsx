import { getUserRole } from '@/lib/auth';
import { redirect } from 'next/navigation';
import DashboardNav from '@/components/dashboard/dashboard-nav';

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const role = await getUserRole();

  // For now, skip user check to avoid API errors
  // TODO: Uncomment after Auth0 API is configured
  // const user = await getCurrentUser();
  // if (user && !user.isOnboarded) {
  //   redirect('/onboarding');
  // }

  return (
    <div className="min-h-screen bg-gray-50">
      <DashboardNav user={null} role={role} />
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {children}
      </main>
    </div>
  );
}
