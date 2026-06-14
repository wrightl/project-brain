'use client';

export default function AdminAppError({
    error,
    reset,
}: {
    error: Error & { digest?: string };
    reset: () => void;
}) {
    return (
        <div className="mx-auto max-w-2xl px-4 py-16 text-center">
            <h2 className="text-xl font-semibold text-gray-900">
                Admin section error
            </h2>
            <p className="mt-2 text-gray-600">
                {error.message || 'Failed to load the admin page.'}
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
