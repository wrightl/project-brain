'use client';

export default function UserAppError({
    error,
    reset,
}: {
    error: Error & { digest?: string };
    reset: () => void;
}) {
    return (
        <div className="mx-auto max-w-2xl px-4 py-16 text-center">
            <h2 className="text-xl font-semibold text-gray-900">
                Something went wrong
            </h2>
            <p className="mt-2 text-gray-600">
                {error.message || 'An unexpected error occurred.'}
            </p>
            <button
                type="button"
                onClick={reset}
                className="mt-6 rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
            >
                Try again
            </button>
        </div>
    );
}
