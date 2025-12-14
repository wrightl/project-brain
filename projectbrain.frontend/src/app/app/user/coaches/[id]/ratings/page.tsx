'use client';

import { useParams, useRouter } from 'next/navigation';
import {
    useCoachRatings,
    useMyCoachRating,
} from '@/_hooks/queries/use-coach-ratings';
import StarRating from '@/_components/coach/star-rating';
import { useState } from 'react';
import RatingForm from '@/_components/coach/rating-form';
import { ArrowLeftIcon } from '@heroicons/react/24/outline';

export default function CoachRatingsPage() {
    const params = useParams();
    const router = useRouter();
    const coachId = params.id as string;
    const [page, setPage] = useState(1);
    const [showRatingForm, setShowRatingForm] = useState(false);

    const { data, isLoading, error } = useCoachRatings(coachId, {
        page,
        pageSize: 10,
    });
    const { data: myRating, isLoading: isLoadingMyRating } =
        useMyCoachRating(coachId);

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
                        Error loading ratings. Please try again.
                    </p>
                </div>
            </div>
        );
    }

    const ratings = data?.items || [];
    const totalPages = data?.totalPages || 0;

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
                        Coach Ratings
                    </h1>
                    {data && (
                        <p className="text-sm text-gray-600 mt-1">
                            {data.totalCount} total{' '}
                            {data.totalCount === 1 ? 'rating' : 'ratings'}
                        </p>
                    )}
                </div>
            </div>

            {/* Rating Form Toggle */}
            <div className="bg-white shadow rounded-lg p-6">
                {!showRatingForm ? (
                    <div className="space-y-4">
                        {!isLoadingMyRating && myRating ? (
                            <div className="border border-indigo-200 bg-indigo-50 rounded-lg p-4 mb-4">
                                <div className="flex items-center justify-between mb-2">
                                    <div>
                                        <h3 className="text-sm font-medium text-indigo-900">
                                            Your Rating
                                        </h3>
                                        <p className="text-xs text-indigo-700 mt-1">
                                            You can update your rating at any
                                            time
                                        </p>
                                    </div>
                                    <StarRating
                                        rating={myRating.rating}
                                        size="md"
                                    />
                                </div>
                                {myRating.feedback && (
                                    <p className="text-sm text-indigo-800 mt-2">
                                        "{myRating.feedback}"
                                    </p>
                                )}
                                <p className="text-xs text-indigo-600 mt-2">
                                    Last updated:{' '}
                                    {new Date(
                                        myRating.updatedAt
                                    ).toLocaleDateString()}
                                </p>
                            </div>
                        ) : (
                            <div className="border border-gray-200 bg-gray-50 rounded-lg p-4 mb-4">
                                <p className="text-sm text-gray-700">
                                    You can only rate this coach once. If you've
                                    already rated, you can update your rating
                                    below.
                                </p>
                            </div>
                        )}
                        <button
                            onClick={() => setShowRatingForm(true)}
                            className="w-full px-4 py-2 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 transition-colors"
                        >
                            {myRating
                                ? 'Update Your Rating'
                                : 'Add Your Rating'}
                        </button>
                    </div>
                ) : (
                    <div>
                        <div className="flex items-center justify-between mb-4">
                            <h2 className="text-lg font-semibold text-gray-900">
                                {myRating
                                    ? 'Update Your Rating'
                                    : 'Rate This Coach'}
                            </h2>
                            <button
                                onClick={() => setShowRatingForm(false)}
                                className="text-sm text-gray-600 hover:text-gray-800"
                            >
                                Cancel
                            </button>
                        </div>
                        <RatingForm
                            coachId={coachId}
                            onSuccess={() => {
                                setShowRatingForm(false);
                            }}
                        />
                    </div>
                )}
            </div>

            {/* Ratings List */}
            {ratings.length === 0 ? (
                <div className="bg-white shadow rounded-lg p-12 text-center">
                    <p className="text-gray-500">
                        No ratings yet. Be the first to rate this coach!
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
