import PageHeader from '@/_components/page-header';
import {
    ChatBubbleLeftRightIcon,
    UserGroupIcon,
    CloudArrowUpIcon,
    MusicalNoteIcon,
    ShieldCheckIcon,
    ChartBarIcon,
    SparklesIcon,
    DocumentTextIcon,
    ClockIcon,
    GlobeAltIcon,
} from '@heroicons/react/24/outline';

export default function FeaturesPage() {
    const userFeatures = [
        {
            name: 'AI Chat Assistant',
            description:
                'Have natural conversations with our AI assistant trained to understand and support neurodivergent individuals. Get instant answers, guidance, and emotional support 24/7.',
            icon: ChatBubbleLeftRightIcon,
            details: [
                'Unlimited conversations (on Pro and Ultimate tiers)',
                'Context-aware responses',
                'Personalized based on your uploaded documents',
                'Voice input support (Pro and Ultimate tiers)',
            ],
        },
        {
            name: 'Voice Notes',
            description:
                'Record voice notes that are automatically transcribed. Express yourself naturally without the pressure of typing, and let our AI process your thoughts.',
            icon: MusicalNoteIcon,
            details: [
                'High-quality voice recording',
                'Automatic transcription',
                'AI analysis of voice notes',
                'Easy playback and review',
            ],
        },
        {
            name: 'Document Management',
            description:
                'Upload your own documents, resources, and files to create a personalized knowledge base. Our AI learns from your content to provide more relevant and personalized support.',
            icon: CloudArrowUpIcon,
            details: [
                'Upload multiple file formats',
                'Organize your personal library',
                'AI learns from your documents',
                'Secure cloud storage',
            ],
        },
        {
            name: 'Coach Connections',
            description:
                'Find and connect with expert coaches in your area who specialize in supporting neurodivergent individuals. Build meaningful relationships and receive personalized guidance.',
            icon: UserGroupIcon,
            details: [
                'Search for coaches by location',
                'View coach profiles and specialties',
                'Secure messaging system',
                'Schedule and manage connections',
            ],
        },
        {
            name: 'Usage Analytics',
            description:
                'Track your usage patterns, monitor your progress, and understand how you interact with the platform. Use insights to optimize your experience and growth.',
            icon: ChartBarIcon,
            details: [
                'Daily and monthly usage statistics',
                'Conversation history',
                'Progress tracking',
                'Personalized insights',
            ],
        },
        {
            name: 'Subscription Management',
            description:
                'Choose from flexible subscription tiers that match your needs. Upgrade or downgrade anytime, with transparent pricing and feature access.',
            icon: SparklesIcon,
            details: [
                'Free tier with essential features',
                'Pro tier with unlimited access',
                'Ultimate tier with premium features',
                'Easy subscription management',
            ],
        },
    ];

    const securityFeatures = [
        {
            name: 'Enterprise-Grade Security',
            description:
                'Your data is protected with industry-leading security measures including encryption, secure authentication, and regular security audits.',
            icon: ShieldCheckIcon,
        },
        {
            name: 'Privacy First',
            description:
                'Your conversations and personal information remain completely confidential. We never share your data with third parties without your explicit consent.',
            icon: ShieldCheckIcon,
        },
        {
            name: 'Secure Authentication',
            description:
                'Protected by Auth0, one of the most trusted authentication platforms. Your account is secured with modern authentication standards.',
            icon: ShieldCheckIcon,
        },
    ];

    const advancedFeatures = [
        {
            name: 'Research Reports',
            description:
                'Generate comprehensive research reports on topics that matter to you. Get detailed insights and analysis tailored to your needs (Pro and Ultimate tiers).',
            icon: DocumentTextIcon,
        },
        {
            name: 'External Integrations',
            description:
                'Connect with your favorite tools like Google Calendar, Notion, and more. Streamline your workflow and keep everything in sync (Ultimate tier).',
            icon: GlobeAltIcon,
        },
        {
            name: 'Priority Support',
            description:
                'Get faster response times and dedicated support channels. Ultimate tier members receive 24/7 real-time chat support.',
            icon: ClockIcon,
        },
    ];

    return (
        <div className="min-h-screen bg-slate-900">
            <PageHeader />
            <div className="pt-32 pb-16">
                {/* Hero Section */}
                <div className="mx-auto max-w-7xl px-6 lg:px-8 py-16">
                    <div className="mx-auto max-w-3xl text-center">
                        <h1 className="text-4xl font-bold tracking-tight text-white sm:text-6xl">
                            Features
                        </h1>
                        <p className="mt-6 text-lg leading-8 text-gray-300">
                            Discover all the powerful features that make
                            ProjectBrain the complete support platform for
                            neurodivergent individuals.
                        </p>
                    </div>
                </div>

                {/* Main Features Section */}
                <div className="py-24 sm:py-32 bg-slate-800">
                    <div className="mx-auto max-w-7xl px-6 lg:px-8">
                        <div className="mx-auto max-w-2xl lg:text-center">
                            <h2 className="text-base font-semibold leading-7 text-fuchsia-300">
                                Core Features
                            </h2>
                            <h3 className="mt-2 text-3xl font-bold tracking-tight text-fuchsia-300 sm:text-4xl">
                                Everything you need to thrive
                            </h3>
                            <p className="mt-6 text-lg leading-8 text-gray-300">
                                ProjectBrain offers a comprehensive suite of
                                features designed to support your unique journey.
                                From AI-powered conversations to coach
                                connections, we provide the tools you need.
                            </p>
                        </div>
                        <div className="mx-auto mt-16 max-w-2xl sm:mt-20 lg:mt-24 lg:max-w-none">
                            <dl className="grid max-w-xl grid-cols-1 gap-x-8 gap-y-16 lg:max-w-none lg:grid-cols-2">
                                {userFeatures.map((feature) => {
                                    const Icon = feature.icon;
                                    return (
                                        <div
                                            key={feature.name}
                                            className="flex flex-col"
                                        >
                                            <dt className="flex items-center gap-x-3 text-base font-semibold leading-7 text-gray-100">
                                                <Icon
                                                    className="h-6 w-6 flex-none text-fuchsia-300"
                                                    aria-hidden="true"
                                                />
                                                <span className="text-violet-100">
                                                    {feature.name}
                                                </span>
                                            </dt>
                                            <dd className="mt-4 flex flex-auto flex-col text-base leading-7 text-gray-300">
                                                <p className="flex-auto">
                                                    {feature.description}
                                                </p>
                                                {feature.details && (
                                                    <ul className="mt-4 space-y-2 list-disc list-inside text-sm text-gray-400">
                                                        {feature.details.map(
                                                            (detail, index) => (
                                                                <li key={index}>
                                                                    {detail}
                                                                </li>
                                                            )
                                                        )}
                                                    </ul>
                                                )}
                                            </dd>
                                        </div>
                                    );
                                })}
                            </dl>
                        </div>
                    </div>
                </div>

                {/* Security Features */}
                <div className="py-24 sm:py-32">
                    <div className="mx-auto max-w-7xl px-6 lg:px-8">
                        <div className="mx-auto max-w-2xl lg:text-center">
                            <h2 className="text-base font-semibold leading-7 text-fuchsia-300">
                                Security & Privacy
                            </h2>
                            <h3 className="mt-2 text-3xl font-bold tracking-tight text-fuchsia-300 sm:text-4xl">
                                Your data is protected
                            </h3>
                            <p className="mt-6 text-lg leading-8 text-gray-300">
                                We take your privacy and security seriously. All
                                your data is encrypted, secured, and kept
                                completely confidential.
                            </p>
                        </div>
                        <div className="mx-auto mt-16 max-w-2xl sm:mt-20 lg:mt-24 lg:max-w-none">
                            <dl className="grid max-w-xl grid-cols-1 gap-x-8 gap-y-16 lg:max-w-none lg:grid-cols-3">
                                {securityFeatures.map((feature) => {
                                    const Icon = feature.icon;
                                    return (
                                        <div
                                            key={feature.name}
                                            className="flex flex-col"
                                        >
                                            <dt className="flex items-center gap-x-3 text-base font-semibold leading-7 text-gray-100">
                                                <Icon
                                                    className="h-6 w-6 flex-none text-fuchsia-300"
                                                    aria-hidden="true"
                                                />
                                                <span className="text-violet-100">
                                                    {feature.name}
                                                </span>
                                            </dt>
                                            <dd className="mt-4 flex flex-auto flex-col text-base leading-7 text-gray-300">
                                                <p className="flex-auto">
                                                    {feature.description}
                                                </p>
                                            </dd>
                                        </div>
                                    );
                                })}
                            </dl>
                        </div>
                    </div>
                </div>

                {/* Advanced Features */}
                <div className="py-24 sm:py-32 bg-slate-800">
                    <div className="mx-auto max-w-7xl px-6 lg:px-8">
                        <div className="mx-auto max-w-2xl lg:text-center">
                            <h2 className="text-base font-semibold leading-7 text-fuchsia-300">
                                Premium Features
                            </h2>
                            <h3 className="mt-2 text-3xl font-bold tracking-tight text-fuchsia-300 sm:text-4xl">
                                Unlock advanced capabilities
                            </h3>
                            <p className="mt-6 text-lg leading-8 text-gray-300">
                                Upgrade to Pro or Ultimate tier to access
                                advanced features like research reports, external
                                integrations, and priority support.
                            </p>
                        </div>
                        <div className="mx-auto mt-16 max-w-2xl sm:mt-20 lg:mt-24 lg:max-w-none">
                            <dl className="grid max-w-xl grid-cols-1 gap-x-8 gap-y-16 lg:max-w-none lg:grid-cols-3">
                                {advancedFeatures.map((feature) => {
                                    const Icon = feature.icon;
                                    return (
                                        <div
                                            key={feature.name}
                                            className="flex flex-col"
                                        >
                                            <dt className="flex items-center gap-x-3 text-base font-semibold leading-7 text-gray-100">
                                                <Icon
                                                    className="h-6 w-6 flex-none text-fuchsia-300"
                                                    aria-hidden="true"
                                                />
                                                <span className="text-violet-100">
                                                    {feature.name}
                                                </span>
                                            </dt>
                                            <dd className="mt-4 flex flex-auto flex-col text-base leading-7 text-gray-300">
                                                <p className="flex-auto">
                                                    {feature.description}
                                                </p>
                                            </dd>
                                        </div>
                                    );
                                })}
                            </dl>
                        </div>
                    </div>
                </div>

                {/* CTA Section */}
                <div className="py-24 sm:py-32">
                    <div className="mx-auto max-w-7xl px-6 lg:px-8">
                        <div className="mx-auto max-w-2xl text-center">
                            <h2 className="text-3xl font-bold tracking-tight text-fuchsia-300 sm:text-4xl">
                                Ready to get started?
                            </h2>
                            <p className="mt-6 text-lg leading-8 text-gray-300">
                                Explore our pricing plans and choose the tier that
                                best fits your needs.
                            </p>
                            <div className="mt-10 flex items-center justify-center gap-x-6">
                                <a
                                    href="/pricing"
                                    className="rounded-md bg-fuchsia-300 px-6 py-3 text-sm font-semibold text-white shadow-sm hover:opacity-90"
                                >
                                    View Pricing
                                </a>
                                <a
                                    href="/auth/login?returnTo=/app"
                                    className="text-sm font-semibold leading-6 text-fuchsia-300"
                                >
                                    Sign up for free{' '}
                                    <span
                                        className="text-violet-100"
                                        aria-hidden="true"
                                    >
                                        →
                                    </span>
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

