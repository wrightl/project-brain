'use client';

import { ExclamationCircleIcon, ArrowPathIcon } from '@heroicons/react/24/outline';

interface ErrorRetryProps {
    error: Error | string;
    onRetry: () => void;
    title?: string;
}

/**
 * Component for displaying errors with retry functionality
 */
export function ErrorRetry({ error, onRetry, title = 'Something went wrong' }: ErrorRetryProps) {
    const errorMessage = typeof error === 'string' ? error : error.message;

    return (
        <div className="bg-red-50 border border-red-200 rounded-lg p-6" role="alert">
            <div className="flex items-start">
                <div className="flex-shrink-0">
                    <ExclamationCircleIcon
                        className="h-5 w-5 text-red-600"
                        aria-hidden="true"
                    />
                </div>
                <div className="ml-3 flex-1">
                    <h3 className="text-sm font-medium text-red-800">{title}</h3>
                    <div className="mt-2 text-sm text-red-700">
                        <p>{errorMessage}</p>
                    </div>
                    <div className="mt-4">
                        <button
                            type="button"
                            onClick={onRetry}
                            className="inline-flex items-center px-3 py-2 border border-transparent text-sm leading-4 font-medium rounded-md text-red-800 bg-red-100 hover:bg-red-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
                            aria-label="Retry"
                        >
                            <ArrowPathIcon className="h-4 w-4 mr-2" aria-hidden="true" />
                            Try again
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}

