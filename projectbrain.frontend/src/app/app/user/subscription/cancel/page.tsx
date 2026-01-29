'use client';

import Link from 'next/link';

export default function SubscriptionCancelPage() {
    return (
        <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4">
            <div className="max-w-md w-full bg-white shadow-lg rounded-lg p-8 text-center">
                <div className="text-gray-400 text-6xl mb-4">✕</div>
                <h1 className="text-3xl font-bold text-gray-900 mb-4">
                    Checkout Cancelled
                </h1>
                <p className="text-gray-600 mb-8">
                    Your subscription checkout was cancelled. No charges have
                    been made.
                </p>
                <p className="text-sm text-gray-500 mb-8">
                    If you changed your mind or encountered an issue, you can
                    try again at any time.
                </p>

                <div className="space-y-3">
                    <Link
                        href="/pricing"
                        className="block w-full px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 text-center font-medium"
                    >
                        View Pricing Plans
                    </Link>
                    <Link
                        href="/app/user/subscription"
                        className="block w-full px-4 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 text-center"
                    >
                        Go to Subscription Management
                    </Link>
                    <Link
                        href="/app/user"
                        className="block w-full px-4 py-2 text-indigo-600 hover:text-indigo-700 text-center text-sm"
                    >
                        Return to Dashboard
                    </Link>
                </div>
            </div>
        </div>
    );
}
