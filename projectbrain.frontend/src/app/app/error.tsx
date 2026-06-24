'use client';

import Link from 'next/link';

export default function AppError({
    error,
    reset,
}: {
    error: Error & { digest?: string };
    reset: () => void;
}) {
    return (
        <div className="mx-auto max-w-2xl px-4 py-16 text-center">
            <h2 className="text-xl font-semibold text-gray-900">
                Couldn&apos;t load your profile
            </h2>
            <p className="mt-2 text-gray-600">
                {error.message ||
                    'Check your connection and try again.'}
            </p>
            <div className="mt-6 flex flex-col items-center gap-3 sm:flex-row sm:justify-center">
                <button
                    type="button"
                    onClick={reset}
                    className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
                >
                    Try again
                </button>
                <Link
                    href="/auth/logout"
                    className="text-sm font-medium text-gray-600 hover:text-gray-900"
                >
                    Log out
                </Link>
            </div>
        </div>
    );
}
