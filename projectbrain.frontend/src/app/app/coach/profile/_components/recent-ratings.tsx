'use client';

import { useCoachRatings } from '@/_hooks/queries/use-coach-ratings';
import StarRating from '@/_components/coach/star-rating';
import Link from 'next/link';
import { ArrowRightIcon } from '@heroicons/react/24/outline';

interface RecentRatingsProps {
    coachId: string;
}

export default function RecentRatings({ coachId }: RecentRatingsProps) {
    const { data, isLoading, error } = useCoachRatings(coachId, {
        page: 1,
        pageSize: 3,
    });

    if (isLoading) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="animate-pulse space-y-4">
                    <div className="h-6 bg-gray-200 rounded w-1/3"></div>
                    <div className="h-20 bg-gray-200 rounded"></div>
                    <div className="h-20 bg-gray-200 rounded"></div>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                    <p className="text-sm text-red-800">
                        Error loading ratings. Please try again.
                    </p>
                </div>
            </div>
        );
    }

    const ratings = data?.items || [];
    const totalCount = data?.totalCount || 0;

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <div className="flex items-center justify-between mb-4">
                <div>
                    <h2 className="text-lg font-semibold text-gray-900">
                        Recent Ratings
                    </h2>
                    {totalCount > 0 && (
                        <p className="text-sm text-gray-600 mt-1">
                            {totalCount} total{' '}
                            {totalCount === 1 ? 'rating' : 'ratings'}
                        </p>
                    )}
                </div>
                {totalCount > 0 && (
                    <Link
                        href="/app/coach/profile/ratings"
                        className="text-sm font-medium text-indigo-600 hover:text-indigo-700 flex items-center gap-1"
                    >
                        View All
                        <ArrowRightIcon className="h-4 w-4" />
                    </Link>
                )}
            </div>

            {ratings.length === 0 ? (
                <div className="text-center py-8">
                    <p className="text-gray-500 text-sm">
                        No ratings yet. Share your profile to start receiving
                        feedback!
                    </p>
                </div>
            ) : (
                <div className="space-y-4">
                    {ratings.map((rating) => (
                        <div
                            key={rating.id}
                            className="border-b border-gray-200 pb-4 last:border-0 last:pb-0"
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
        </div>
    );
}
