import { PlanCards, type Plan } from './plan-cards';

const USER_PLANS: Plan[] = [
    {
        name: 'Free',
        description: 'Everything you need to try a daily rhythm with your buddy.',
        features: [
            '50 AI queries per day',
            '200 AI queries per month',
            '3 coach connections',
            '200 messages to coaches per month',
            '20 uploaded files',
            '100MB of uploaded files',
        ],
        cta: 'Sign up free',
    },
    {
        name: 'Pro',
        description: 'No limits on your buddy or your coaches, and talk instead of type.',
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
        cta: 'Start free trial',
        featured: true,
    },
    {
        name: 'Ultimate',
        description: 'For founders who lean on ProjectBrain every day.',
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
        cta: 'Get started',
    },
];

export default function UserTiers() {
    return <PlanCards plans={USER_PLANS} />;
}
