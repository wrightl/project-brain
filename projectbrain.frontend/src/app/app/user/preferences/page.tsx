import { Metadata } from 'next';
import { UserService } from '@/_services/user-service';
import { User } from '@/_lib/types';
import PreferencesSection from '../profile/_components/preferences-section';

export const metadata: Metadata = {
    title: 'Preferences',
    description: 'Manage your preferences and settings',
};

export default async function PreferencesPage() {
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
                <h1 className="text-3xl font-bold text-gray-900">Preferences</h1>
                <p className="mt-2 text-sm text-gray-600">
                    Customize your app experience and settings
                </p>
            </div>
            <PreferencesSection user={user as User} />
        </div>
    );
}

