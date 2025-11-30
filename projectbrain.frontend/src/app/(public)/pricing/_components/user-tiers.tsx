'use client';

import Link from 'next/link';

export default function UserTiers() {
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
            cta: 'Get Started',
            ctaLink: '/auth/signup',
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
            cta: 'Start Free Trial',
            ctaLink: '/auth/signup',
            highlight: true,
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
            cta: 'Get Started',
            ctaLink: '/auth/signup',
        },
    ];

    return (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {tiers.map((tier) => (
                <div
                    key={tier.name}
                    className={`border-2 rounded-lg p-8 ${
                        tier.highlight
                            ? 'border-blue-600 bg-blue-50 shadow-lg'
                            : 'border-gray-200 bg-white'
                    }`}
                >
                    {tier.highlight && (
                        <div className="text-center mb-4">
                            <span className="bg-blue-600 text-white px-3 py-1 rounded-full text-sm font-semibold">
                                Most Popular
                            </span>
                        </div>
                    )}

                    <h3 className="text-2xl font-bold mb-4">{tier.name}</h3>

                    {tier.name === 'Free' ? (
                        <div className="text-3xl font-semibold mb-6">
                            {tier.price}
                        </div>
                    ) : (
                        <div className="mb-6">
                            <div className="text-3xl font-semibold">
                                {tier.monthlyPrice}
                                <span className="text-lg font-normal text-gray-600">
                                    /month
                                </span>
                            </div>
                            <div className="text-sm text-gray-600 mt-2">
                                {tier.annualPrice} when paid annually
                            </div>
                            <div className="text-sm text-gray-500 mt-1">
                                ({tier.annualTotal})
                            </div>
                        </div>
                    )}

                    <ul className="space-y-3 mb-8">
                        {tier.features.map((feature, index) => (
                            <li key={index} className="flex items-start">
                                <span className="text-green-600 mr-2 mt-1">
                                    ✓
                                </span>
                                <span>{feature}</span>
                            </li>
                        ))}
                    </ul>

                    <Link
                        href={tier.ctaLink}
                        className={`block w-full text-center px-6 py-3 rounded-lg font-semibold ${
                            tier.highlight
                                ? 'bg-blue-600 text-white hover:bg-blue-700'
                                : 'bg-gray-200 text-gray-800 hover:bg-gray-300'
                        }`}
                    >
                        {tier.cta}
                    </Link>
                </div>
            ))}
        </div>
    );
}
