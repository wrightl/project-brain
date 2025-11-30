'use client';

import Link from 'next/link';

export default function CoachTiers() {
    const tiers = [
        {
            name: 'Free',
            price: '$0',
            features: ['3 client connections', '10 client messages per month'],
            cta: 'Get Started',
            ctaLink: '/auth/signup',
        },
        {
            name: 'Pro',
            monthlyPrice: '$60',
            annualPrice: '$50/month',
            annualTotal: '$600/year',
            features: [
                'Unlimited client connections',
                'Unlimited client messages',
                '7-day free trial',
            ],
            cta: 'Start Free Trial',
            ctaLink: '/auth/signup',
            highlight: true,
        },
    ];

    return (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8 max-w-4xl mx-auto">
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
