'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { UserOnboardingData } from '@/_lib/types';

interface UserOnboardingFormProps {
    userEmail: string;
}

export default function UserOnboardingForm({
    userEmail,
}: UserOnboardingFormProps) {
    const router = useRouter();
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [formData, setFormData] = useState({
        email: userEmail,
        fullName: '',
        doB: '',
        favoriteColor: '',
        preferredPronoun: '',
        neurodivergentDetails: '',
    });

    const handleChange = (
        e: React.ChangeEvent<
            HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
        >
    ) => {
        setFormData((prev) => ({
            ...prev,
            [e.target.name]: e.target.value,
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setIsSubmitting(true);

        try {
            const data: UserOnboardingData = {
                email: formData.email,
                fullName: formData.fullName,
                doB: formData.doB,
                favoriteColor: formData.favoriteColor,
                preferredPronoun: formData.preferredPronoun,
                neurodivergentDetails: formData.neurodivergentDetails,
            };

            // Call the Next.js API route instead of backend directly
            const response = await fetch('/api/users/onboard', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(data),
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(
                    errorData.error || 'Failed to complete onboarding'
                );
            }

            // Success - redirect to dashboard
            router.push('/dashboard');
            router.refresh();
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : 'Failed to complete onboarding'
            );
            setIsSubmitting(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                    {error}
                </div>
            )}

            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                <div>
                    <label
                        htmlFor="fullName"
                        className="block text-sm font-medium text-gray-700"
                    >
                        Full Name *
                    </label>
                    <input
                        type="text"
                        id="fullName"
                        name="fullName"
                        required
                        value={formData.fullName}
                        onChange={handleChange}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>

                <div>
                    <label
                        htmlFor="email"
                        className="block text-sm font-medium text-gray-700"
                    >
                        Email *
                    </label>
                    <input
                        type="email"
                        id="email"
                        name="email"
                        required
                        value={formData.email}
                        onChange={handleChange}
                        disabled
                        className="mt-1 block w-full rounded-md border-gray-300 bg-gray-50 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm cursor-not-allowed"
                    />
                </div>

                <div>
                    <label
                        htmlFor="doB"
                        className="block text-sm font-medium text-gray-700"
                    >
                        Date of Birth *
                    </label>
                    <input
                        type="date"
                        id="doB"
                        name="doB"
                        required
                        value={formData.doB}
                        onChange={handleChange}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>

                <div>
                    <label
                        htmlFor="favoriteColor"
                        className="block text-sm font-medium text-gray-700"
                    >
                        Favorite Color *
                    </label>
                    <input
                        type="text"
                        id="favoriteColor"
                        name="favoriteColor"
                        required
                        value={formData.favoriteColor}
                        onChange={handleChange}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>
            </div>

            <div>
                <label
                    htmlFor="preferredPronoun"
                    className="block text-sm font-medium text-gray-700"
                >
                    Preferred Pronouns *
                </label>
                <select
                    id="preferredPronoun"
                    name="preferredPronoun"
                    required
                    value={formData.preferredPronoun}
                    onChange={handleChange}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                >
                    <option value="">Select pronouns</option>
                    <option value="he/him">He/Him</option>
                    <option value="she/her">She/Her</option>
                    <option value="they/them">They/Them</option>
                    <option value="other">Other/Prefer to self-describe</option>
                </select>
            </div>

            <div>
                <label
                    htmlFor="neurodivergentDetails"
                    className="block text-sm font-medium text-gray-700"
                >
                    Neurodivergent Traits or Diagnoses
                </label>
                <textarea
                    id="neurodivergentDetails"
                    name="neurodivergentDetails"
                    rows={4}
                    placeholder="Share any neurodivergent traits, diagnoses, or information you'd like us to know (optional)..."
                    value={formData.neurodivergentDetails}
                    onChange={handleChange}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                />
                <p className="mt-1 text-xs text-gray-500">
                    This information helps us provide better support. It&apos;s
                    completely optional and will be kept confidential.
                </p>
            </div>

            <div className="flex justify-end">
                <button
                    type="submit"
                    disabled={isSubmitting}
                    className="px-6 py-3 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                >
                    {isSubmitting ? 'Saving...' : 'Complete Profile'}
                </button>
            </div>
        </form>
    );
}
