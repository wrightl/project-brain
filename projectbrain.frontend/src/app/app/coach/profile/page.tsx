import { Metadata } from 'next';
import { Coach } from '@/_lib/types';
import { UserService } from '@/_services/user-service';
import { callBackendApi } from '@/_lib/backend-api';
import CoachProfileForm from './_components/coach-profile-form';
import CoachSubscriptionSummary from './_components/subscription-summary';

export const metadata: Metadata = {
    title: 'Profile',
    description: 'View and edit your coach profile',
};

export default async function CoachProfilePage() {
    let coach: Coach | null = null;
    let error: string | null = null;

    try {
        const user = await UserService.getCurrentUser();

        if (!user) {
            error = 'User not found';
        } else {
            // Check if user is a coach
            const isCoach = user.roles?.includes('coach');
            if (!isCoach) {
                error = 'User is not a coach';
            } else {
                // Fetch coach profile data
                const coachResponse = await callBackendApi(
                    `/coaches/${user.id}`
                );
                if (coachResponse.ok) {
                    coach = await coachResponse.json();
                } else {
                    error = 'Failed to load coach profile';
                }
            }
        }
    } catch (err) {
        error = err instanceof Error ? err.message : 'Failed to load profile';
    }

    if (error || !coach) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <p className="text-gray-600">
                    {error || 'Coach profile not found.'}
                </p>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-3xl font-bold text-gray-900">My Profile</h1>
                <p className="mt-2 text-sm text-gray-600">
                    View and manage your coach profile information
                </p>
            </div>
            <CoachSubscriptionSummary />
            <CoachProfileForm coach={coach} />
        </div>
    );
}
