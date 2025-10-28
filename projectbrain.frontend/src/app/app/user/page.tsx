import { Metadata } from 'next';
import UserDashboard from './_components/user-dashboard';

export const metadata: Metadata = {
    title: 'Dashboard',
    description: 'Your personal dashboard',
};

export default function UserDashboardPage() {
    return <UserDashboard />;
}
