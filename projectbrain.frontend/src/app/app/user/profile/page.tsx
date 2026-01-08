import { Metadata } from 'next';
import { UserService } from '@/_services/user-service';
import { User } from '@/_lib/types';
import ProfileForm from './_components/profile-form';

export const metadata: Metadata = {
    title: 'Profile',
    description: 'View and edit your profile',
};

export default async function ProfilePage() {
    const user = await UserService.getCurrentUser();

    if (!user) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <p className="text-gray-600">User not found.</p>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-3xl font-bold text-gray-900">My Profile</h1>
                <p className="mt-2 text-sm text-gray-600">
                    View and manage your profile information
                </p>
            </div>
            <ProfileForm user={user as User} />
        </div>
    );
}
