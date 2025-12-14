'use client';

import { StarIcon } from '@heroicons/react/24/solid';
import { StarIcon as StarOutlineIcon } from '@heroicons/react/24/outline';

interface StarRatingProps {
    rating: number;
    maxRating?: number;
    size?: 'sm' | 'md' | 'lg';
    interactive?: boolean;
    onRatingChange?: (rating: number) => void;
    showValue?: boolean;
}

export default function StarRating({
    rating,
    maxRating = 5,
    size = 'md',
    interactive = false,
    onRatingChange,
    showValue = false,
}: StarRatingProps) {
    const sizeClasses = {
        sm: 'h-4 w-4',
        md: 'h-5 w-5',
        lg: 'h-6 w-6',
    };

    const handleClick = (value: number) => {
        if (interactive && onRatingChange) {
            onRatingChange(value);
        }
    };

    const handleMouseEnter = (value: number) => {
        if (interactive && onRatingChange) {
            // Optional: Add hover effect
        }
    };

    return (
        <div className="flex items-center gap-1">
            <div className="flex items-center">
                {Array.from({ length: maxRating }, (_, i) => {
                    const value = i + 1;
                    const isFilled = value <= Math.round(rating);

                    return (
                        <button
                            key={i}
                            type="button"
                            onClick={() => handleClick(value)}
                            onMouseEnter={() => handleMouseEnter(value)}
                            disabled={!interactive}
                            className={`${
                                interactive
                                    ? 'cursor-pointer hover:scale-110 transition-transform'
                                    : 'cursor-default'
                            } ${sizeClasses[size]}`}
                        >
                            {isFilled ? (
                                <StarIcon className="text-yellow-400" />
                            ) : (
                                <StarOutlineIcon className="text-gray-300" />
                            )}
                        </button>
                    );
                })}
            </div>
            {showValue && (
                <span className="ml-2 text-sm text-gray-600">
                    {rating.toFixed(1)}
                </span>
            )}
        </div>
    );
}

