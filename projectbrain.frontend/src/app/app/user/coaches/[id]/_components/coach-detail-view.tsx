'use client';

import { useRouter } from 'next/navigation';
import {
    MapPinIcon,
    EnvelopeIcon,
    AcademicCapIcon,
    UserGroupIcon,
    SparklesIcon,
} from '@heroicons/react/24/outline';
import { Coach } from '@/_lib/types';
import { useState } from 'react';
import AvailabilityBadge from '@/_components/coach/availability-badge';

interface CoachDetailViewProps {
    coach: Coach;
}

export default function CoachDetailView({ coach }: CoachDetailViewProps) {
    const router = useRouter();

    const handleContactCoach = async () => {
        router.push(`/app/user/messages/${coach.coachProfileId}`);
    };

    return (
        <div className="max-w-4xl mx-auto space-y-6">
            {/* Header */}
            <div className="bg-white shadow rounded-lg p-6">
                <div className="flex items-start justify-between">
                    <div className="flex-1">
                        <div className="flex items-center gap-3">
                            <h1 className="text-3xl font-bold text-gray-900">
                                {coach.fullName}
                            </h1>
                            {coach.availabilityStatus && (
                                <AvailabilityBadge
                                    status={coach.availabilityStatus}
                                />
                            )}
                        </div>
                        {coach.city && (
                            <p className="mt-2 text-gray-600 flex items-center">
                                <MapPinIcon className="h-5 w-5 mr-2 text-gray-400" />
                                {[
                                    coach.city,
                                    coach.stateProvince,
                                    coach.country,
                                ]
                                    .filter(Boolean)
                                    .join(', ')}
                            </p>
                        )}
                        {coach.email && (
                            <p className="mt-1 text-gray-600 flex items-center">
                                <EnvelopeIcon className="h-5 w-5 mr-2 text-gray-400" />
                                {coach.email}
                            </p>
                        )}
                    </div>
                    <button
                        onClick={handleContactCoach}
                        className="px-6 py-3 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 transition-colors disabled:bg-gray-300 disabled:cursor-not-allowed"
                    >
                        Contact Coach
                    </button>
                </div>
            </div>

            {/* Qualifications */}
            {coach.qualifications.length > 0 && (
                <div className="bg-white shadow rounded-lg p-6">
                    <h2 className="text-xl font-semibold text-gray-900 mb-4 flex items-center">
                        <AcademicCapIcon className="h-6 w-6 mr-2 text-indigo-600" />
                        Qualifications
                    </h2>
                    <ul className="space-y-2">
                        {coach.qualifications.map((qualification, index) => (
                            <li
                                key={index}
                                className="flex items-start text-gray-700"
                            >
                                <span className="mr-2 text-indigo-600">•</span>
                                <span>{qualification}</span>
                            </li>
                        ))}
                    </ul>
                </div>
            )}

            {/* Specialisms */}
            {coach.specialisms.length > 0 && (
                <div className="bg-white shadow rounded-lg p-6">
                    <h2 className="text-xl font-semibold text-gray-900 mb-4 flex items-center">
                        <SparklesIcon className="h-6 w-6 mr-2 text-indigo-600" />
                        Specialisms
                    </h2>
                    <div className="flex flex-wrap gap-2">
                        {coach.specialisms.map((specialism) => (
                            <span
                                key={specialism}
                                className="px-3 py-1 bg-indigo-100 text-indigo-800 text-sm font-medium rounded-full"
                            >
                                {specialism}
                            </span>
                        ))}
                    </div>
                </div>
            )}

            {/* Age Groups */}
            {coach.ageGroups.length > 0 && (
                <div className="bg-white shadow rounded-lg p-6">
                    <h2 className="text-xl font-semibold text-gray-900 mb-4 flex items-center">
                        <UserGroupIcon className="h-6 w-6 mr-2 text-indigo-600" />
                        Age Groups
                    </h2>
                    <div className="flex flex-wrap gap-2">
                        {coach.ageGroups.map((ageGroup) => (
                            <span
                                key={ageGroup}
                                className="px-3 py-1 bg-green-100 text-green-800 text-sm font-medium rounded-full"
                            >
                                {ageGroup}
                            </span>
                        ))}
                    </div>
                </div>
            )}

            {/* Contact Section */}
            <div className="bg-indigo-50 border border-indigo-200 rounded-lg p-6">
                <h3 className="text-lg font-semibold text-indigo-900 mb-2">
                    Ready to get started?
                </h3>
                <p className="text-sm text-indigo-700 mb-4">
                    Contact {coach.fullName} to discuss how they can help you.
                    You can chat via text or voice.
                </p>
                <button
                    onClick={handleContactCoach}
                    className="px-6 py-3 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 transition-colors disabled:bg-gray-300 disabled:cursor-not-allowed"
                >
                    Start Conversation
                </button>
            </div>
        </div>
    );
}
