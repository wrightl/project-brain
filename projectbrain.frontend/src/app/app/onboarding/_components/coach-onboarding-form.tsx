'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { onboardUser } from '@/_lib/api-client';
import { CoachOnboardingData } from '@/_lib/types';

interface CoachOnboardingFormProps {
    userEmail: string;
    accessToken: string;
}

export default function CoachOnboardingForm({
    userEmail,
    accessToken,
}: CoachOnboardingFormProps) {
    const router = useRouter();
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [formData, setFormData] = useState({
        email: userEmail,
        fullName: '',
        doB: '',
        favoriteColor: '',
        address: '',
        experience: '',
    });

    const handleChange = (
        e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
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
                doB: formData.doB,
                favoriteColor: formData.favoriteColor,
                address: formData.address,
                experience: formData.experience,
            };

            await onboardUser(data);
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
                    htmlFor="address"
                    className="block text-sm font-medium text-gray-700"
                >
                    Address (City, State/Region) *
                </label>
                <input
                    type="text"
                    id="address"
                    name="address"
                    required
                    placeholder="e.g., San Francisco, CA"
                    value={formData.address}
                    onChange={handleChange}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                />
                <p className="mt-1 text-xs text-gray-500">
                    This helps us connect you with users in your geographical
                    region
                </p>
            </div>

            <div>
                <label
                    htmlFor="experience"
                    className="block text-sm font-medium text-gray-700"
                >
                    Experience & Qualifications *
                </label>
                <textarea
                    id="experience"
                    name="experience"
                    required
                    rows={4}
                    placeholder="Tell us about your coaching experience, certifications, and areas of expertise..."
                    value={formData.experience}
                    onChange={handleChange}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                />
                <p className="mt-1 text-xs text-gray-500">
                    Share your background, relevant certifications, and
                    experience working with neurodivergent individuals
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
