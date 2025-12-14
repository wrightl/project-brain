'use client';

import { useState, useEffect } from 'react';
import { useCreateOrUpdateCoachRating, useMyCoachRating } from '@/_hooks/queries/use-coach-ratings';
import StarRating from './star-rating';

interface RatingFormProps {
    coachId: string;
    onSuccess?: () => void;
}

export default function RatingForm({ coachId, onSuccess }: RatingFormProps) {
    const [rating, setRating] = useState<number>(0);
    const [feedback, setFeedback] = useState<string>('');
    const [isSubmitting, setIsSubmitting] = useState(false);

    const { data: myRating } = useMyCoachRating(coachId);
    const createOrUpdateMutation = useCreateOrUpdateCoachRating(coachId);

    // Initialize form with existing rating if available
    useEffect(() => {
        if (myRating) {
            setRating(myRating.rating);
            setFeedback(myRating.feedback || '');
        }
    }, [myRating]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (rating === 0) {
            alert('Please select a rating');
            return;
        }

        setIsSubmitting(true);
        try {
            await createOrUpdateMutation.mutateAsync({
                rating,
                feedback: feedback.trim() || undefined,
            });
            if (onSuccess) {
                onSuccess();
            }
        } catch (error) {
            console.error('Error submitting rating:', error);
            alert('Failed to submit rating. Please try again.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                    Rating *
                </label>
                <StarRating
                    rating={rating}
                    interactive={true}
                    onRatingChange={setRating}
                    size="lg"
                />
            </div>

            <div>
                <label
                    htmlFor="feedback"
                    className="block text-sm font-medium text-gray-700 mb-2"
                >
                    Feedback (optional)
                </label>
                <textarea
                    id="feedback"
                    rows={4}
                    value={feedback}
                    onChange={(e) => setFeedback(e.target.value)}
                    className="w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    placeholder="Share your experience with this coach..."
                    maxLength={2000}
                />
                <p className="mt-1 text-xs text-gray-500">
                    {feedback.length}/2000 characters
                </p>
            </div>

            <button
                type="submit"
                disabled={isSubmitting || rating === 0}
                className="w-full px-4 py-2 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 transition-colors disabled:bg-gray-300 disabled:cursor-not-allowed"
            >
                {isSubmitting
                    ? 'Submitting...'
                    : myRating
                    ? 'Update Rating'
                    : 'Submit Rating'}
            </button>
        </form>
    );
}

