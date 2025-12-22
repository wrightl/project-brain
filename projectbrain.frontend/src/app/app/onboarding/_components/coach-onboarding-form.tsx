'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { CoachOnboardingData } from '@/_lib/types';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

interface CoachOnboardingFormProps {
    userEmail: string;
}

export default function CoachOnboardingForm({
    userEmail,
}: CoachOnboardingFormProps) {
    const router = useRouter();
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [formData, setFormData] = useState({
        email: userEmail,
        fullName: '',
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
            const data: CoachOnboardingData = {
                email: formData.email,
                fullName: formData.fullName,
            };

            console.log(`Onboarding data ${JSON.stringify(data)}`);

            const response = await fetchWithAuth('/api/coach/onboard', {
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

            router.push('/app');
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
            </div>

            <div className="flex justify-end">
                <button
                    type="submit"
                    disabled={isSubmitting}
                    className="px-6 py-3 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                >
                    {isSubmitting ? 'Saving...' : 'Complete Coach Profile'}
                </button>
            </div>
        </form>
    );
}
