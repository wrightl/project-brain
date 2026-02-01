'use client';

import { useEffect, useMemo, useState } from 'react';
import { Dialog, DialogPanel, DialogTitle } from '@headlessui/react';
import toast from 'react-hot-toast';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import type { Subscription } from '@/_lib/types';

interface ReferralSettings {
    enabled: boolean;
    maxRewardsPerInviter: number;
    inviterFreeMonths: number;
    inviteeFreeMonths: number;
    inviteTokenExpiryDays: number;
    maxInvitesPerRequest: number;
    requireInviterActiveSubscriberToEarn: boolean;
}

interface ReferralInviteListItem {
    id: string;
    recipientEmail: string;
    status: string;
    sentAt: string;
    lastSentAt?: string | null;
    resendCount: number;
    expiresAt: string;
    acceptedAt?: string | null;
    rewardedAt?: string | null;
}

interface CreateInvitesResponse {
    created: ReferralInviteListItem[];
    skipped: { recipientEmail: string; reason: string }[];
}

function normalizeEmail(email: string) {
    return email.trim().toLowerCase();
}

function isValidEmail(email: string) {
    // Simple front-end validation; server is authoritative.
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
}

function formatDate(dateString?: string | null) {
    if (!dateString) return '—';
    return new Date(dateString).toLocaleDateString();
}

function statusLabel(status: string) {
    switch (status) {
        case 'Pending':
            return 'Pending';
        case 'Accepted':
            return 'Accepted';
        case 'Rewarded':
            return 'Rewarded';
        case 'Expired':
            return 'Expired';
        default:
            return status;
    }
}

function statusClasses(status: string) {
    switch (status) {
        case 'Pending':
            return 'bg-yellow-50 text-yellow-800 border-yellow-200';
        case 'Accepted':
            return 'bg-blue-50 text-blue-800 border-blue-200';
        case 'Rewarded':
            return 'bg-green-50 text-green-800 border-green-200';
        case 'Expired':
            return 'bg-gray-50 text-gray-800 border-gray-200';
        default:
            return 'bg-gray-50 text-gray-800 border-gray-200';
    }
}

export default function ReferralsSection({
    subscription,
}: {
    subscription: Subscription | null;
}) {
    const [settings, setSettings] = useState<ReferralSettings | null>(null);
    const [invites, setInvites] = useState<ReferralInviteListItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [isModalOpen, setIsModalOpen] = useState(false);
    const [emailInput, setEmailInput] = useState('');
    const [emails, setEmails] = useState<string[]>([]);
    const [sending, setSending] = useState(false);

    const isPayingSubscriber = useMemo(() => {
        if (!subscription) return false;
        if ((subscription.tier || 'Free') === 'Free') return false;
        if ((subscription.status || '').toLowerCase() !== 'active') return false;
        if (subscription.trialEndsAt) {
            const trialEnds = new Date(subscription.trialEndsAt).getTime();
            if (!Number.isNaN(trialEnds) && trialEnds > Date.now()) return false;
        }
        return true;
    }, [subscription]);

    const maxInvitesPerRequest = Math.min(
        10,
        Math.max(1, settings?.maxInvitesPerRequest ?? 10)
    );

    const invitedEmailSet = useMemo(() => {
        const set = new Set<string>();
        for (const invite of invites) {
            set.add(normalizeEmail(invite.recipientEmail));
        }
        return set;
    }, [invites]);

    const load = async () => {
        try {
            setLoading(true);
            setError(null);
            const [settingsRes, invitesRes] = await Promise.all([
                fetchWithAuth('/api/referrals/settings'),
                fetchWithAuth('/api/referrals/invites'),
            ]);

            if (!settingsRes.ok) {
                throw new Error('Failed to load referral settings');
            }
            if (!invitesRes.ok) {
                throw new Error('Failed to load referral invites');
            }

            const settingsData: ReferralSettings = await settingsRes.json();
            const invitesData: ReferralInviteListItem[] = await invitesRes.json();

            setSettings(settingsData);
            setInvites(invitesData);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Failed to load referrals');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load();
    }, []);

    const addEmailsFromText = (text: string) => {
        const parts = text
            .split(/[\s,;]+/g)
            .map((p) => p.trim())
            .filter(Boolean);

        if (parts.length === 0) return;

        const next: string[] = [...emails];
        for (const part of parts) {
            if (next.length >= maxInvitesPerRequest) break;
            const normalized = normalizeEmail(part);
            if (!isValidEmail(normalized)) continue;
            if (invitedEmailSet.has(normalized)) continue;
            if (next.some((e) => normalizeEmail(e) === normalized)) continue;
            next.push(normalized);
        }
        setEmails(next);
    };

    const handleAddEmail = () => {
        addEmailsFromText(emailInput);
        setEmailInput('');
    };

    const handleRemoveEmail = (email: string) => {
        setEmails((prev) => prev.filter((e) => e !== email));
    };

    const handleSendInvites = async () => {
        if (!settings?.enabled) return;
        if (emails.length === 0) {
            toast.error('Add at least one email address');
            return;
        }
        setSending(true);
        try {
            const response = await fetchWithAuth('/api/referrals/invites', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ emails }),
            });
            if (!response.ok) {
                const payload = await response.json().catch(() => null);
                throw new Error(payload?.error || 'Failed to send invites');
            }

            const payload: CreateInvitesResponse = await response.json();
            const createdCount = payload.created?.length ?? 0;
            const skippedCount = payload.skipped?.length ?? 0;

            if (createdCount > 0) {
                toast.success(
                    `Sent ${createdCount} invite${createdCount === 1 ? '' : 's'}`
                );
            }
            if (skippedCount > 0) {
                toast(
                    `Skipped ${skippedCount} (${payload.skipped
                        .map((s) => `${s.recipientEmail}: ${s.reason}`)
                        .join(', ')})`
                );
            }

            setIsModalOpen(false);
            setEmails([]);
            setEmailInput('');
            await load();
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Failed to send invites');
        } finally {
            setSending(false);
        }
    };

    const handleResend = async (inviteId: string) => {
        try {
            const response = await fetchWithAuth(
                `/api/referrals/invites/${encodeURIComponent(inviteId)}/resend`,
                { method: 'POST' }
            );
            if (!response.ok) {
                const payload = await response.json().catch(() => null);
                throw new Error(payload?.error || 'Failed to resend invite');
            }
            toast.success('Invite resent');
            await load();
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Failed to resend invite');
        }
    };

    if (loading) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="animate-pulse">
                    <div className="h-6 bg-gray-200 rounded w-1/4 mb-4"></div>
                    <div className="space-y-2">
                        <div className="h-4 bg-gray-200 rounded"></div>
                        <div className="h-4 bg-gray-200 rounded"></div>
                    </div>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="text-red-600">
                    Error: {error}
                </div>
            </div>
        );
    }

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <div className="flex items-start justify-between gap-4">
                <div>
                    <h2 className="text-2xl font-semibold mb-1 text-gray-900">
                        Referrals
                    </h2>
                    <p className="text-sm text-gray-600">
                        Invite friends by email. When they become a paying
                        subscriber (after the free trial), they’ll receive free
                        months. {isPayingSubscriber ? 'You can also earn free months.' : ''}
                    </p>
                </div>
                <button
                    type="button"
                    disabled={!settings?.enabled}
                    onClick={() => setIsModalOpen(true)}
                    className="px-4 py-2 rounded-md text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-400"
                >
                    Invite friends
                </button>
            </div>

            {!settings?.enabled && (
                <div className="mt-4 text-sm text-gray-700">
                    The referral program is currently disabled.
                </div>
            )}

            <div className="mt-6">
                <h3 className="text-sm font-semibold text-gray-900 mb-2">
                    Your invites
                </h3>
                {invites.length === 0 ? (
                    <div className="text-sm text-gray-600">
                        You haven’t invited anyone yet.
                    </div>
                ) : (
                    <div className="overflow-x-auto">
                        <table className="min-w-full text-sm">
                            <thead>
                                <tr className="text-left text-gray-600 border-b border-gray-200">
                                    <th className="py-2 pr-4">Email</th>
                                    <th className="py-2 pr-4">Status</th>
                                    <th className="py-2 pr-4">Sent</th>
                                    <th className="py-2 pr-4">Accepted</th>
                                    <th className="py-2 pr-4">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {invites.map((invite) => (
                                    <tr
                                        key={invite.id}
                                        className="border-b border-gray-100"
                                    >
                                        <td className="py-3 pr-4 text-gray-900">
                                            {invite.recipientEmail}
                                        </td>
                                        <td className="py-3 pr-4">
                                            <span
                                                className={`inline-flex items-center px-2 py-1 rounded border text-xs ${statusClasses(
                                                    invite.status
                                                )}`}
                                            >
                                                {statusLabel(invite.status)}
                                            </span>
                                        </td>
                                        <td className="py-3 pr-4 text-gray-700">
                                            {formatDate(
                                                invite.lastSentAt || invite.sentAt
                                            )}
                                        </td>
                                        <td className="py-3 pr-4 text-gray-700">
                                            {formatDate(invite.acceptedAt)}
                                        </td>
                                        <td className="py-3 pr-4">
                                            {invite.status === 'Pending' && (
                                                <button
                                                    type="button"
                                                    onClick={() =>
                                                        handleResend(invite.id)
                                                    }
                                                    className="text-indigo-600 hover:text-indigo-700"
                                                >
                                                    Resend
                                                </button>
                                            )}
                                            {invite.status !== 'Pending' && (
                                                <span className="text-gray-400">
                                                    —
                                                </span>
                                            )}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            <Dialog
                open={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                className="relative z-50"
            >
                <div
                    className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
                    aria-hidden="true"
                />

                <div className="fixed inset-0 z-10 overflow-y-auto">
                    <div className="flex min-h-full items-end justify-center p-4 text-center sm:items-center sm:p-0">
                        <DialogPanel className="relative transform overflow-hidden rounded-lg bg-white text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-lg">
                            <div className="px-4 pb-4 pt-5 sm:p-6 sm:pb-4">
                                <DialogTitle
                                    as="h3"
                                    className="text-base font-semibold leading-6 text-gray-900"
                                >
                                    Invite friends
                                </DialogTitle>

                                <div className="mt-2 text-sm text-gray-600">
                                    <p>
                                        Invite someone by email. If they become
                                        a paying subscriber (after the free
                                        trial), they’ll receive{' '}
                                        <strong>
                                            {settings?.inviteeFreeMonths ?? 0}{' '}
                                            free month
                                            {(settings?.inviteeFreeMonths ??
                                                0) === 1
                                                ? ''
                                                : 's'}
                                        </strong>
                                        .
                                    </p>
                                    {isPayingSubscriber && (
                                        <p className="mt-2">
                                            You’ll receive{' '}
                                            <strong>
                                                {settings?.inviterFreeMonths ??
                                                    0}{' '}
                                                free month
                                                {(settings?.inviterFreeMonths ??
                                                    0) === 1
                                                    ? ''
                                                    : 's'}
                                            </strong>{' '}
                                            (up to{' '}
                                            <strong>
                                                {settings?.maxRewardsPerInviter ??
                                                    0}
                                            </strong>{' '}
                                            rewards).
                                        </p>
                                    )}
                                    <p className="mt-2">
                                        You can add up to{' '}
                                        <strong>{maxInvitesPerRequest}</strong>{' '}
                                        email addresses per send.
                                    </p>
                                </div>

                                <div className="mt-4">
                                    <label className="block text-sm font-medium text-gray-700">
                                        Add email addresses
                                    </label>
                                    <div className="mt-2 flex gap-2">
                                        <input
                                            type="email"
                                            value={emailInput}
                                            onChange={(e) =>
                                                setEmailInput(e.target.value)
                                            }
                                            onKeyDown={(e) => {
                                                if (e.key === 'Enter') {
                                                    e.preventDefault();
                                                    handleAddEmail();
                                                }
                                            }}
                                            placeholder="name@example.com"
                                            className="flex-1 rounded-md border border-gray-300 shadow-sm px-3 py-2 text-sm focus:border-indigo-500 focus:ring-indigo-500"
                                            disabled={
                                                emails.length >=
                                                maxInvitesPerRequest
                                            }
                                        />
                                        <button
                                            type="button"
                                            onClick={handleAddEmail}
                                            className="px-3 py-2 rounded-md text-sm font-medium bg-gray-100 hover:bg-gray-200 text-gray-900"
                                            disabled={
                                                emails.length >=
                                                maxInvitesPerRequest
                                            }
                                        >
                                            Add
                                        </button>
                                    </div>
                                    <p className="mt-1 text-xs text-gray-500">
                                        Tip: paste multiple emails separated by
                                        spaces or commas.
                                    </p>

                                    {emails.length > 0 && (
                                        <div className="mt-3 flex flex-wrap gap-2">
                                            {emails.map((email) => (
                                                <span
                                                    key={email}
                                                    className="inline-flex items-center gap-2 rounded-full border border-gray-200 px-3 py-1 text-xs text-gray-800"
                                                >
                                                    {email}
                                                    <button
                                                        type="button"
                                                        onClick={() =>
                                                            handleRemoveEmail(
                                                                email
                                                            )
                                                        }
                                                        className="text-gray-500 hover:text-gray-700"
                                                        aria-label={`Remove ${email}`}
                                                    >
                                                        ×
                                                    </button>
                                                </span>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            </div>

                            <div className="bg-gray-50 px-4 py-3 sm:flex sm:flex-row-reverse sm:px-6">
                                <button
                                    type="button"
                                    onClick={handleSendInvites}
                                    disabled={sending || emails.length === 0}
                                    className="inline-flex w-full justify-center rounded-md px-3 py-2 text-sm font-semibold text-white shadow-sm sm:ml-3 sm:w-auto bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-400"
                                >
                                    {sending ? 'Sending...' : 'Send invites'}
                                </button>
                                <button
                                    type="button"
                                    onClick={() => setIsModalOpen(false)}
                                    className="mt-3 inline-flex w-full justify-center rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 hover:bg-gray-50 sm:mt-0 sm:w-auto"
                                >
                                    Cancel
                                </button>
                            </div>
                        </DialogPanel>
                    </div>
                </div>
            </Dialog>
        </div>
    );
}

