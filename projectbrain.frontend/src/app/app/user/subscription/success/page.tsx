'use client';

import { useEffect, useState } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { apiClient } from '@/_lib/api-client';
import Link from 'next/link';

interface SessionVerification {
    sessionId: string;
    paymentStatus: string;
    status: string;
    subscription: {
        id: string;
        tier: string;
        status: string;
        trialEndsAt?: string;
        currentPeriodStart: string;
        currentPeriodEnd: string;
    } | null;
    tier: string | null;
}

export default function SubscriptionSuccessPage() {
    const searchParams = useSearchParams();
    const router = useRouter();
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [sessionData, setSessionData] = useState<SessionVerification | null>(
        null,
    );

    useEffect(() => {
        const sessionId = searchParams.get('session_id');

        if (!sessionId) {
            setError('No session ID provided');
            setLoading(false);
            return;
        }

        const verifySession = async () => {
            try {
                setLoading(true);
                setError(null);

                const data = await apiClient<SessionVerification>(
                    `/api/subscriptions/verify-session?session_id=${encodeURIComponent(sessionId)}`,
                );

                setSessionData(data);
            } catch (err) {
                console.error('Failed to verify session:', err);
                setError(
                    err instanceof Error
                        ? err.message
                        : 'Failed to verify checkout session',
                );
            } finally {
                setLoading(false);
            }
        };

        verifySession();
    }, [searchParams]);

    if (loading) {
        return (
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 mx-auto mb-4"></div>
                    <p className="text-gray-600">
                        Verifying your subscription...
                    </p>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <div className="max-w-md w-full bg-white shadow-lg rounded-lg p-8 text-center">
                    <div className="text-red-600 text-5xl mb-4">✕</div>
                    <h1 className="text-2xl font-bold text-gray-900 mb-4">
                        Verification Failed
                    </h1>
                    <p className="text-gray-600 mb-6">{error}</p>
                    <div className="space-y-3">
                        <Link
                            href="/app/user/subscription"
                            className="block w-full px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 text-center"
                        >
                            Go to Subscription Management
                        </Link>
                        <Link
                            href="/pricing"
                            className="block w-full px-4 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 text-center"
                        >
                            View Pricing
                        </Link>
                    </div>
                </div>
            </div>
        );
    }

    if (!sessionData) {
        return (
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <div className="max-w-md w-full bg-white shadow-lg rounded-lg p-8 text-center">
                    <p className="text-gray-600">No session data available</p>
                    <Link
                        href="/app/user/subscription"
                        className="mt-4 inline-block px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700"
                    >
                        Go to Subscription Management
                    </Link>
                </div>
            </div>
        );
    }

    const isSuccess =
        sessionData.paymentStatus === 'paid' ||
        sessionData.status === 'complete';
    const tier =
        sessionData.subscription?.tier || sessionData.tier || 'Unknown';

    return (
        <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4">
            <div className="max-w-2xl w-full bg-white shadow-lg rounded-lg p-8">
                {isSuccess ? (
                    <>
                        <div className="text-center mb-8">
                            <div className="text-green-600 text-6xl mb-4">
                                ✓
                            </div>
                            <h1 className="text-3xl font-bold text-gray-900 mb-2">
                                Subscription Successful!
                            </h1>
                            <p className="text-gray-600">
                                Your subscription has been activated
                                successfully.
                            </p>
                        </div>

                        {sessionData.subscription && (
                            <div className="bg-gray-50 rounded-lg p-6 mb-6">
                                <h2 className="text-xl font-semibold text-gray-900 mb-4">
                                    Subscription Details
                                </h2>
                                <dl className="space-y-3">
                                    <div className="flex justify-between">
                                        <dt className="text-gray-600">Plan:</dt>
                                        <dd className="font-semibold text-gray-900">
                                            {tier}
                                        </dd>
                                    </div>
                                    <div className="flex justify-between">
                                        <dt className="text-gray-600">
                                            Status:
                                        </dt>
                                        <dd className="font-semibold text-green-600 capitalize">
                                            {sessionData.subscription.status}
                                        </dd>
                                    </div>
                                    {sessionData.subscription.trialEndsAt && (
                                        <div className="flex justify-between">
                                            <dt className="text-gray-600">
                                                Trial Ends:
                                            </dt>
                                            <dd className="font-semibold text-gray-900">
                                                {new Date(
                                                    sessionData.subscription
                                                        .trialEndsAt,
                                                ).toLocaleDateString()}
                                            </dd>
                                        </div>
                                    )}
                                    <div className="flex justify-between">
                                        <dt className="text-gray-600">
                                            Current Period:
                                        </dt>
                                        <dd className="font-semibold text-gray-900">
                                            {new Date(
                                                sessionData.subscription
                                                    .currentPeriodStart,
                                            ).toLocaleDateString()}{' '}
                                            -{' '}
                                            {new Date(
                                                sessionData.subscription
                                                    .currentPeriodEnd,
                                            ).toLocaleDateString()}
                                        </dd>
                                    </div>
                                </dl>
                            </div>
                        )}

                        <div className="space-y-3">
                            <Link
                                href="/app/user/subscription"
                                className="block w-full px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 text-center font-medium"
                            >
                                Manage Subscription
                            </Link>
                            <Link
                                href="/app"
                                className="block w-full px-4 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 text-center"
                            >
                                Go to Dashboard
                            </Link>
                        </div>
                    </>
                ) : (
                    <>
                        <div className="text-center mb-8">
                            <div className="text-yellow-600 text-5xl mb-4">
                                ⚠
                            </div>
                            <h1 className="text-2xl font-bold text-gray-900 mb-2">
                                Payment Pending
                            </h1>
                            <p className="text-gray-600">
                                Your subscription is being processed. You will
                                receive an email confirmation once payment is
                                complete.
                            </p>
                        </div>

                        <div className="space-y-3">
                            <Link
                                href="/app/user/subscription"
                                className="block w-full px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 text-center font-medium"
                            >
                                Check Subscription Status
                            </Link>
                            <Link
                                href="/app"
                                className="block w-full px-4 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 text-center"
                            >
                                Go to Dashboard
                            </Link>
                        </div>
                    </>
                )}
            </div>
        </div>
    );
}
