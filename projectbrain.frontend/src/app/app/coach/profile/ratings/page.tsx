'use client';

import { useRouter } from 'next/navigation';
import {
    useCoachRatings,
    useMyPersonalCoachRatings,
} from '@/_hooks/queries/use-coach-ratings';
import StarRating from '@/_components/coach/star-rating';
import { useState, useEffect } from 'react';
import { ArrowLeftIcon } from '@heroicons/react/24/outline';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { Coach } from '@/_lib/types';

export default function CoachRatingsPage() {
    const router = useRouter();
    // const [coachId, setCoachId] = useState<string | null>(null);
    const [page, setPage] = useState(1);

    // useEffect(() => {
    //     async function loadCoach() {
    //         try {
    //             const response = await fetchWithAuth('/api/user/me');
    //             if (response.ok) {
    //                 const user = (await response.json()) as Coach;
    //                 if (user.coachProfileId) {
    //                     setCoachId(user.coachProfileId.toString());
    //                 }
    //             }
    //         } catch (error) {
    //             console.error('Error loading coach:', error);
    //         } finally {
    //             setIsLoadingCoach(false);
    //         }
    //     }
    //     loadCoach();
    // }, []);

    const { data, isLoading, error } = useMyPersonalCoachRatings({
        page,
        pageSize: 10,
    });

    if (isLoading) {
        return (
            <div className="max-w-4xl mx-auto p-6">
                <div className="animate-pulse space-y-4">
                    <div className="h-8 bg-gray-200 rounded w-1/4"></div>
                    <div className="h-32 bg-gray-200 rounded"></div>
                    <div className="h-32 bg-gray-200 rounded"></div>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="max-w-4xl mx-auto p-6">
                <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                    <p className="text-sm text-red-800">
                        {error
                            ? 'Error loading ratings. Please try again.'
                            : 'Coach profile not found.'}
                    </p>
                </div>
            </div>
        );
    }

    // TODO: Sort out the response from the API. Should be a paged response.
    const ratings = data || [];
    const totalPages = data?.totalPages || 0;
    const totalCount = data?.totalCount || 0;

    return (
        <div className="max-w-4xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center gap-4">
                <button
                    onClick={() => router.back()}
                    className="p-2 hover:bg-gray-100 rounded-full transition-colors"
                >
                    <ArrowLeftIcon className="h-6 w-6 text-gray-600" />
                </button>
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        My Ratings
                    </h1>
                    {data && (
                        <p className="text-sm text-gray-600 mt-1">
                            {totalCount} total{' '}
                            {totalCount === 1 ? 'rating' : 'ratings'}
                        </p>
                    )}
                </div>
            </div>

            {/* Ratings List */}
            {ratings.length === 0 ? (
                <div className="bg-white shadow rounded-lg p-12 text-center">
                    <p className="text-gray-500">
                        No ratings yet. Share your profile to start receiving
                        feedback!
                    </p>
                </div>
            ) : (
                <div className="space-y-4">
                    {ratings.map((rating) => (
                        <div
                            key={rating.id}
                            className="bg-white shadow rounded-lg p-6"
                        >
                            <div className="flex items-start justify-between">
                                <div className="flex-1">
                                    <div className="flex items-center gap-3 mb-2">
                                        <StarRating
                                            rating={rating.rating}
                                            size="sm"
                                        />
                                        <span className="text-sm font-medium text-gray-900">
                                            {rating.userName}
                                        </span>
                                        <span className="text-xs text-gray-500">
                                            {new Date(
                                                rating.createdAt
                                            ).toLocaleDateString()}
                                        </span>
                                        {rating.updatedAt !==
                                            rating.createdAt && (
                                            <span className="text-xs text-gray-400">
                                                (updated{' '}
                                                {new Date(
                                                    rating.updatedAt
                                                ).toLocaleDateString()}
                                                )
                                            </span>
                                        )}
                                    </div>
                                    {rating.feedback && (
                                        <p className="text-sm text-gray-700 mt-2">
                                            {rating.feedback}
                                        </p>
                                    )}
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
                <div className="flex items-center justify-center gap-2">
                    <button
                        onClick={() => setPage((p) => Math.max(1, p - 1))}
                        disabled={page === 1}
                        className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        Previous
                    </button>
                    <span className="text-sm text-gray-700">
                        Page {page} of {totalPages}
                    </span>
                    <button
                        onClick={() =>
                            setPage((p) => Math.min(totalPages, p + 1))
                        }
                        disabled={page === totalPages}
                        className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        Next
                    </button>
                </div>
            )}
        </div>
    );
}
