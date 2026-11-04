import { PlanCards, type Plan } from './plan-cards';

const COACH_PLANS: Plan[] = [
    {
        name: 'Free',
        description: 'Set up your profile and start working with a few clients.',
        features: ['3 client connections', '10 client messages per month'],
        cta: 'Join as a coach',
    },
    {
        name: 'Pro',
        description: 'For coaches with a growing client list.',
        monthlyPrice: '$60',
        annualPrice: '$50/month',
        annualTotal: '$600/year',
        features: [
            'Unlimited client connections',
            'Unlimited client messages',
            '7-day free trial',
        ],
        cta: 'Start free trial',
        featured: true,
    },
];

export default function CoachTiers() {
    return <PlanCards plans={COACH_PLANS} />;
}
