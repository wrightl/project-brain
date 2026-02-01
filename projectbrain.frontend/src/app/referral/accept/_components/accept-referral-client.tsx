'use client';

import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';
import toast from 'react-hot-toast';

interface ReferralInvitePreview {
    inviterName: string;
    inviteeFreeMonths: number;
    isExpired: boolean;
    expiresAt: string;
}

export default function AcceptReferralClient({
    token,
    isAuthenticated,
}: {
    token: string;
    isAuthenticated: boolean;
}) {
    const [preview, setPreview] = useState<ReferralInvitePreview | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [accepting, setAccepting] = useState(false);
    const [accepted, setAccepted] = useState(false);

    const returnTo = useMemo(() => {
        const path = `/referral/accept?token=${encodeURIComponent(
            token || '',
        )}`;
        return `/auth/login?returnTo=${encodeURIComponent(path)}`;
    }, [token]);

    useEffect(() => {
        const run = async () => {
            try {
                setLoading(true);
                setError(null);

                if (!token) {
                    setError('Missing invite token.');
                    return;
                }

                const res = await fetch(
                    `/api/referrals/preview?token=${encodeURIComponent(token)}`,
                    { cache: 'no-store' },
                );
                const payload = await res.json().catch(() => null);
                if (!res.ok) {
                    throw new Error(payload?.error || 'Failed to load invite');
                }
                setPreview(payload);
            } catch (e) {
                setError(
                    e instanceof Error ? e.message : 'Failed to load invite',
                );
            } finally {
                setLoading(false);
            }
        };
        run();
    }, [token]);

    const handleAccept = async () => {
        if (!token) return;
        setAccepting(true);
        try {
            const res = await fetch('/api/referrals/accept', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ token }),
            });

            const payload = await res.json().catch(() => null);
            if (!res.ok) {
                throw new Error(payload?.error || 'Failed to accept invite');
            }

            setAccepted(true);
            toast.success('Invite accepted');
        } catch (e) {
            toast.error(
                e instanceof Error ? e.message : 'Failed to accept invite',
            );
        } finally {
            setAccepting(false);
        }
    };

    if (loading) {
        return (
            <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
                <div className="bg-white shadow rounded-lg p-6">
                    <div className="animate-pulse">
                        <div className="h-6 bg-gray-200 rounded w-1/3 mb-4"></div>
                        <div className="space-y-2">
                            <div className="h-4 bg-gray-200 rounded"></div>
                            <div className="h-4 bg-gray-200 rounded"></div>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
                <div className="bg-white shadow rounded-lg p-6">
                    <h1 className="text-2xl font-semibold text-gray-900">
                        Referral invite
                    </h1>
                    <p className="mt-2 text-sm text-red-600">{error}</p>
                </div>
            </div>
        );
    }

    return (
        <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
            <div className="bg-white shadow rounded-lg p-6">
                <h1 className="text-2xl font-semibold text-gray-900">
                    You’ve been invited to ProjectBrain
                </h1>

                <p className="mt-3 text-sm text-gray-700">
                    {preview?.inviterName ? (
                        <>
                            <strong>{preview.inviterName}</strong> invited you
                            to join.
                        </>
                    ) : (
                        <>You’ve been invited to join.</>
                    )}
                </p>

                <div className="mt-4 rounded-md border border-gray-200 p-4">
                    <div className="text-sm text-gray-700">
                        If you become a paying subscriber after the free trial,
                        you’ll receive{' '}
                        <strong className="text-gray-900">
                            {preview?.inviteeFreeMonths ?? 0} free month
                            {(preview?.inviteeFreeMonths ?? 0) === 1 ? '' : 's'}
                        </strong>
                        .
                    </div>
                    <div className="mt-2 text-xs text-gray-500">
                        This invite expires on{' '}
                        {preview?.expiresAt
                            ? new Date(preview.expiresAt).toLocaleDateString()
                            : '—'}
                        .
                    </div>
                </div>

                {preview?.isExpired && (
                    <div className="mt-4 text-sm text-red-600">
                        This invite has expired.
                    </div>
                )}

                {!preview?.isExpired && (
                    <div className="mt-6 flex flex-col sm:flex-row gap-3">
                        {!isAuthenticated ? (
                            <Link
                                href={returnTo}
                                className="inline-flex justify-center rounded-md px-4 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700"
                            >
                                Sign up to accept
                            </Link>
                        ) : accepted ? (
                            <Link
                                href="/app"
                                className="inline-flex justify-center rounded-md px-4 py-2 text-sm font-semibold text-white bg-green-600 hover:bg-green-700"
                            >
                                Continue to the app
                            </Link>
                        ) : (
                            <button
                                type="button"
                                onClick={handleAccept}
                                disabled={accepting}
                                className="inline-flex justify-center rounded-md px-4 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-400"
                            >
                                {accepting ? 'Accepting...' : 'Accept invite'}
                            </button>
                        )}

                        <Link
                            href="/"
                            className="inline-flex justify-center rounded-md px-4 py-2 text-sm font-semibold text-gray-900 bg-white ring-1 ring-inset ring-gray-300 hover:bg-gray-50"
                        >
                            Learn more
                        </Link>
                    </div>
                )}
            </div>
        </div>
    );
}
