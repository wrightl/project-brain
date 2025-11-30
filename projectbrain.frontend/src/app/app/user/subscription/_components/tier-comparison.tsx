'use client';

import { useState } from 'react';
import { apiClient } from '@/_lib/api-client';

interface TierComparisonProps {
    currentTier: string;
    onUpgrade: () => void;
}

export default function TierComparison({ currentTier, onUpgrade }: TierComparisonProps) {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleUpgrade = async (tier: string, isAnnual: boolean) => {
        try {
            setLoading(true);
            setError(null);
            const { url } = await apiClient<{ url: string }>('/api/subscriptions/checkout', {
                method: 'POST',
                body: { tier, isAnnual },
            });
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
            <h2 className="text-2xl font-semibold mb-6">Choose Your Plan</h2>
            
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
                                        {tier.monthlyPrice}
                                        <span className="text-sm font-normal text-gray-600">/month</span>
                                    </div>
                                    <div className="text-sm text-gray-600 mt-1">
                                        {tier.annualPrice} when paid annually ({tier.annualTotal})
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
                                <div className="space-y-2">
                                    <button
                                        onClick={() => handleUpgrade(tier.name, false)}
                                        disabled={loading}
                                        className="w-full px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                                    >
                                        {loading ? 'Loading...' : `Upgrade to ${tier.name} (Monthly)`}
                                    </button>
                                    <button
                                        onClick={() => handleUpgrade(tier.name, true)}
                                        disabled={loading}
                                        className="w-full px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50"
                                    >
                                        {loading ? 'Loading...' : `Upgrade to ${tier.name} (Annual)`}
                                    </button>
                                </div>
                            ) : null}
                        </div>
                    );
                })}
            </div>
        </div>
    );
}

