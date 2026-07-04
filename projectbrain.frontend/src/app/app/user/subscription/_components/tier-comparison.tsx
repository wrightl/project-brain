'use client';

import { useState } from 'react';
import { apiClient } from '@/_lib/api-client';
import { isAllowedCheckoutRedirectUrl } from '@/_lib/url-security';

interface TierComparisonProps {
    currentTier: string;
    onUpgrade: () => void;
}

export default function TierComparison({ currentTier, onUpgrade }: TierComparisonProps) {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [billingInterval, setBillingInterval] = useState<'monthly' | 'annual'>(
        'monthly'
    );

    const handleUpgrade = async (tier: string, isAnnual: boolean) => {
        try {
            setLoading(true);
            setError(null);
            const { url } = await apiClient<{ url: string }>('/api/subscriptions/checkout', {
                method: 'POST',
                body: { tier, isAnnual },
            });
            if (!isAllowedCheckoutRedirectUrl(url)) {
                throw new Error('Invalid checkout redirect URL');
            }
            onUpgrade();
            window.location.href = url;
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to create checkout session');
        } finally {
            setLoading(false);
        }
    };

    const tiers = [
        {
            name: 'Free',
            price: '$0',
            features: [
                '50 AI queries per day',
                '200 AI queries per month',
                '3 coach connections',
                '200 messages to coaches per month',
                '20 uploaded files',
                '100MB of uploaded files',
            ],
        },
        {
            name: 'Pro',
            monthlyPrice: '$12',
            annualPrice: '$10/month',
            annualTotal: '$120/year',
            features: [
                'Unlimited AI queries',
                'Unlimited coach connections',
                'Unlimited messages to coaches',
                'Unlimited files',
                '500MB of uploaded files',
                'Speech input for AI chat',
                '1 free research report per month',
                'Basic support',
                '7-day free trial',
            ],
        },
        {
            name: 'Ultimate',
            monthlyPrice: '$24',
            annualPrice: '$20/month',
            annualTotal: '$240/year',
            features: [
                'Everything in Pro',
                'Unlimited file storage',
                'External integrations',
                'Unlimited research reports',
                'Realtime chat support',
                '24x7 support',
            ],
        },
    ];

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                <h2 className="text-2xl font-semibold">Choose Your Plan</h2>

                <div
                    className="inline-flex rounded-lg border border-gray-300 bg-gray-200 p-1"
                    role="group"
                    aria-label="Billing interval"
                >
                    <button
                        type="button"
                        onClick={() => setBillingInterval('monthly')}
                        aria-pressed={billingInterval === 'monthly'}
                        className={`px-3 py-1.5 text-sm font-medium rounded-md transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 ${
                            billingInterval === 'monthly'
                                ? 'bg-white text-gray-900 shadow-sm'
                                : 'text-gray-700 hover:bg-gray-100 hover:text-gray-900'
                        }`}
                    >
                        Monthly
                    </button>
                    <button
                        type="button"
                        onClick={() => setBillingInterval('annual')}
                        aria-pressed={billingInterval === 'annual'}
                        className={`px-3 py-1.5 text-sm font-medium rounded-md transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 ${
                            billingInterval === 'annual'
                                ? 'bg-white text-gray-900 shadow-sm'
                                : 'text-gray-700 hover:bg-gray-100 hover:text-gray-900'
                        }`}
                    >
                        Annual
                    </button>
                </div>
            </div>
            
            {error && (
                <div className="mb-4 p-4 bg-red-100 text-red-700 rounded">
                    {error}
                </div>
            )}

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                {tiers.map((tier) => {
                    const isCurrentTier = tier.name === currentTier;
                    const isFree = tier.name === 'Free';

                    return (
                        <div
                            key={tier.name}
                            className={`border-2 rounded-lg p-6 ${
                                isCurrentTier
                                    ? 'border-blue-600 bg-blue-50'
                                    : 'border-gray-200'
                            }`}
                        >
                            <h3 className="text-xl font-bold mb-2">{tier.name}</h3>
                            
                            {isFree ? (
                                <div className="text-2xl font-semibold mb-4">{tier.price}</div>
                            ) : (
                                <div className="mb-4">
                                    <div className="text-2xl font-semibold">
                                        {billingInterval === 'monthly'
                                            ? tier.monthlyPrice
                                            : tier.annualPrice}
                                        {billingInterval === 'monthly' && (
                                            <span className="text-sm font-normal text-gray-600">
                                                /month
                                            </span>
                                        )}
                                    </div>
                                    <div className="text-sm text-gray-600 mt-1">
                                        {billingInterval === 'monthly'
                                            ? 'Billed monthly'
                                            : `Billed annually (${tier.annualTotal})`}
                                    </div>
                                </div>
                            )}

                            <ul className="space-y-2 mb-6">
                                {tier.features.map((feature, index) => (
                                    <li key={index} className="flex items-start">
                                        <span className="text-green-600 mr-2">✓</span>
                                        <span className="text-sm">{feature}</span>
                                    </li>
                                ))}
                            </ul>

                            {isCurrentTier ? (
                                <div className="text-center py-2 bg-gray-200 rounded font-medium">
                                    Current Plan
                                </div>
                            ) : !isFree ? (
                                <button
                                    onClick={() =>
                                        handleUpgrade(
                                            tier.name,
                                            billingInterval === 'annual'
                                        )
                                    }
                                    disabled={loading}
                                    className="w-full px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                                >
                                    {loading
                                        ? 'Loading...'
                                        : `Upgrade to ${tier.name} (${
                                              billingInterval === 'annual'
                                                  ? 'Annual'
                                                  : 'Monthly'
                                          })`}
                                </button>
                            ) : null}
                        </div>
                    );
                })}
            </div>
        </div>
    );
}

