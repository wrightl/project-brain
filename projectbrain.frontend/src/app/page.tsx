import Link from 'next/link';
import {
    ChatBubbleLeftRightIcon,
    UserGroupIcon,
    SparklesIcon,
    ShieldCheckIcon,
} from '@heroicons/react/24/outline';

export default function Home() {
    // Don't check session on landing page - let users see the homepage
    // They'll be redirected after clicking "Get Started"

    const features = [
        {
            name: 'AI-Powered Support',
            description:
                'Get personalized assistance from our advanced AI assistant, trained to understand and support neurodivergent individuals.',
            icon: SparklesIcon,
        },
        {
            name: 'Expert Coaches',
            description:
                'Connect with experienced coaches who specialize in supporting neurodivergent people in your local area.',
            icon: UserGroupIcon,
        },
        {
            name: 'Secure & Private',
            description:
                'Your data is protected with enterprise-grade security. Your conversations and information remain completely confidential.',
            icon: ShieldCheckIcon,
        },
        {
            name: 'Personalized Experience',
            description:
                'Upload your own documents and resources to create a truly personalized AI assistant tailored to your needs.',
            icon: ChatBubbleLeftRightIcon,
        },
    ];

    return (
        <div className="bg-white">
            {/* Hero Section */}
            <div className="relative isolate px-6 pt-14 lg:px-8">
                <div className="mx-auto max-w-2xl py-32 sm:py-48 lg:py-56">
                    <div className="text-center">
                        <h1 className="text-4xl font-bold tracking-tight text-gray-900 sm:text-6xl">
                            AI-Powered Support for Neurodivergent Individuals
                        </h1>
                        <p className="mt-6 text-lg leading-8 text-gray-600">
                            Connect with expert coaches and access personalized
                            AI assistance designed to support your unique
                            journey.
                        </p>
                        <div className="mt-10 flex items-center justify-center gap-x-6">
                            <a
                                href="/auth/login?returnTo=/dashboard"
                                className="rounded-md bg-indigo-600 px-6 py-3 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600"
                            >
                                Get Started
                            </a>
                            <Link
                                href="/about"
                                className="text-sm font-semibold leading-6 text-gray-900"
                            >
                                Learn more <span aria-hidden="true">→</span>
                            </Link>
                        </div>
                    </div>
                </div>
            </div>

            {/* Features Section */}
            <div className="py-24 sm:py-32 bg-gray-50">
                <div className="mx-auto max-w-7xl px-6 lg:px-8">
                    <div className="mx-auto max-w-2xl lg:text-center">
                        <h2 className="text-base font-semibold leading-7 text-indigo-600">
                            Everything you need
                        </h2>
                        <p className="mt-2 text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl">
                            Comprehensive support at your fingertips
                        </p>
                        <p className="mt-6 text-lg leading-8 text-gray-600">
                            Whether you&apos;re seeking guidance, looking to
                            connect with coaches, or need AI-powered assistance,
                            ProjectBrain provides the tools and support you
                            need.
                        </p>
                    </div>
                    <div className="mx-auto mt-16 max-w-2xl sm:mt-20 lg:mt-24 lg:max-w-none">
                        <dl className="grid max-w-xl grid-cols-1 gap-x-8 gap-y-16 lg:max-w-none lg:grid-cols-2">
                            {features.map((feature) => {
                                const Icon = feature.icon;
                                return (
                                    <div
                                        key={feature.name}
                                        className="flex flex-col"
                                    >
                                        <dt className="flex items-center gap-x-3 text-base font-semibold leading-7 text-gray-900">
                                            <Icon
                                                className="h-6 w-6 flex-none text-indigo-600"
                                                aria-hidden="true"
                                            />
                                            {feature.name}
                                        </dt>
                                        <dd className="mt-4 flex flex-auto flex-col text-base leading-7 text-gray-600">
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
                        <h2 className="text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl">
                            Ready to get started?
                        </h2>
                        <p className="mt-6 text-lg leading-8 text-gray-600">
                            Join ProjectBrain today and experience personalized
                            support designed for you.
                        </p>
                        <div className="mt-10 flex items-center justify-center gap-x-6">
                            <a
                                href="/auth/login?returnTo=/dashboard"
                                className="rounded-md bg-indigo-600 px-6 py-3 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500"
                            >
                                Sign Up / Log In
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
