import PageHeader from '@/_components/page-header';
import {
    SparklesIcon,
    UserGroupIcon,
    ChatBubbleLeftRightIcon,
    CloudArrowUpIcon,
    MusicalNoteIcon,
    ShieldCheckIcon,
    ChartBarIcon,
} from '@heroicons/react/24/outline';

export default function ProductPage() {
    const capabilities = [
        {
            name: 'AI-Powered Conversations',
            description:
                'Engage in natural, supportive conversations with our advanced AI assistant. Get instant answers, guidance, and support tailored to your unique needs.',
            icon: ChatBubbleLeftRightIcon,
        },
        {
            name: 'Voice Notes',
            description:
                'Record and transcribe voice notes for easy communication. Express yourself naturally and let our AI process your thoughts.',
            icon: MusicalNoteIcon,
        },
        {
            name: 'Personal Document Library',
            description:
                'Upload your own documents, resources, and files to create a personalized knowledge base. Our AI learns from your content to provide more relevant support.',
            icon: CloudArrowUpIcon,
        },
        {
            name: 'Coach Connections',
            description:
                'Connect with expert coaches in your area who specialize in supporting neurodivergent individuals. Build meaningful relationships and get personalized guidance.',
            icon: UserGroupIcon,
        },
        {
            name: 'Usage Analytics',
            description:
                'Track your usage patterns, monitor your progress, and understand how you interact with the platform to optimize your experience.',
            icon: ChartBarIcon,
        },
        {
            name: 'Enterprise Security',
            description:
                'Your data is protected with industry-leading security measures. All conversations and personal information remain completely confidential and secure.',
            icon: ShieldCheckIcon,
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
                            ProjectBrain
                        </h1>
                        <p className="mt-6 text-lg leading-8 text-gray-300">
                            A comprehensive support platform designed specifically
                            for neurodivergent individuals. Connect with expert
                            coaches, access AI-powered assistance, and build a
                            personalized support system that grows with you.
                        </p>
                    </div>
                </div>

                {/* What is ProjectBrain Section */}
                <div className="py-24 sm:py-32 bg-slate-800">
                    <div className="mx-auto max-w-7xl px-6 lg:px-8">
                        <div className="mx-auto max-w-2xl lg:text-center">
                            <h2 className="text-base font-semibold leading-7 text-fuchsia-300">
                                What is ProjectBrain?
                            </h2>
                            <h3 className="mt-2 text-3xl font-bold tracking-tight text-fuchsia-300 sm:text-4xl">
                                Your Personal Support Ecosystem
                            </h3>
                            <p className="mt-6 text-lg leading-8 text-gray-300">
                                ProjectBrain is more than just an app—it&apos;s a
                                complete support system that combines the power of
                                artificial intelligence with human expertise. Whether
                                you need immediate answers, long-term guidance, or
                                a safe space to explore your thoughts, ProjectBrain
                                provides the tools and connections you need to
                                thrive.
                            </p>
                        </div>
                    </div>
                </div>

                {/* Capabilities Section */}
                <div className="py-24 sm:py-32">
                    <div className="mx-auto max-w-7xl px-6 lg:px-8">
                        <div className="mx-auto max-w-2xl lg:text-center">
                            <h2 className="text-base font-semibold leading-7 text-fuchsia-300">
                                Core Capabilities
                            </h2>
                            <h3 className="mt-2 text-3xl font-bold tracking-tight text-fuchsia-300 sm:text-4xl">
                                Everything you need in one platform
                            </h3>
                            <p className="mt-6 text-lg leading-8 text-gray-300">
                                ProjectBrain brings together cutting-edge AI
                                technology, expert human coaches, and powerful
                                personalization tools to create a support system
                                tailored to your unique journey.
                            </p>
                        </div>
                        <div className="mx-auto mt-16 max-w-2xl sm:mt-20 lg:mt-24 lg:max-w-none">
                            <dl className="grid max-w-xl grid-cols-1 gap-x-8 gap-y-16 lg:max-w-none lg:grid-cols-3">
                                {capabilities.map((capability) => {
                                    const Icon = capability.icon;
                                    return (
                                        <div
                                            key={capability.name}
                                            className="flex flex-col"
                                        >
                                            <dt className="flex items-center gap-x-3 text-base font-semibold leading-7 text-gray-100">
                                                <Icon
                                                    className="h-6 w-6 flex-none text-fuchsia-300"
                                                    aria-hidden="true"
                                                />
                                                <span className="text-violet-100">
                                                    {capability.name}
                                                </span>
                                            </dt>
                                            <dd className="mt-4 flex flex-auto flex-col text-base leading-7 text-gray-300">
                                                <p className="flex-auto">
                                                    {capability.description}
                                                </p>
                                            </dd>
                                        </div>
                                    );
                                })}
                            </dl>
                        </div>
                    </div>
                </div>

                {/* Who It's For Section */}
                <div className="py-24 sm:py-32 bg-slate-800">
                    <div className="mx-auto max-w-7xl px-6 lg:px-8">
                        <div className="mx-auto max-w-2xl lg:text-center">
                            <h2 className="text-base font-semibold leading-7 text-fuchsia-300">
                                Who is ProjectBrain for?
                            </h2>
                            <h3 className="mt-2 text-3xl font-bold tracking-tight text-fuchsia-300 sm:text-4xl">
                                Designed for neurodivergent individuals
                            </h3>
                            <p className="mt-6 text-lg leading-8 text-gray-300">
                                ProjectBrain is built specifically for people who
                                think differently. Whether you&apos;re navigating
                                daily challenges, seeking personal growth, or
                                looking for a supportive community, our platform
                                adapts to your unique needs and learning style.
                            </p>
                        </div>
                    </div>
                </div>

                {/* CTA Section */}
                <div className="py-24 sm:py-32">
                    <div className="mx-auto max-w-7xl px-6 lg:px-8">
                        <div className="mx-auto max-w-2xl text-center">
                            <h2 className="text-3xl font-bold tracking-tight text-fuchsia-300 sm:text-4xl">
                                Ready to experience ProjectBrain?
                            </h2>
                            <p className="mt-6 text-lg leading-8 text-gray-300">
                                Join thousands of users who have found support,
                                connection, and growth through ProjectBrain.
                            </p>
                            <div className="mt-10 flex items-center justify-center gap-x-6">
                                <a
                                    href="/auth/login?returnTo=/app"
                                    className="rounded-md bg-fuchsia-300 px-6 py-3 text-sm font-semibold text-white shadow-sm hover:opacity-90"
                                >
                                    Get Started
                                </a>
                                <a
                                    href="/features"
                                    className="text-sm font-semibold leading-6 text-fuchsia-300"
                                >
                                    Learn more about features{' '}
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

