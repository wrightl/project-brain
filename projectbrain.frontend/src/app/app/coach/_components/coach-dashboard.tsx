import Link from 'next/link';
import {
    UsersIcon,
    CalendarIcon,
    ChatBubbleLeftRightIcon,
} from '@heroicons/react/24/outline';
import { getFlags } from '@/_lib/flags';
import { StatisticsService } from '@/_services/statistics-service';

export default async function CoachDashboard() {
    // We added your flag key. The React SDK uses camelCase for flag keys automatically
    // useFlags is a custom hook which returns all feature flags
    const enableCoachSection = (await getFlags())['enable-coach-section'];
    const [
        coachClientsCount,
        pendingClientsCount,
        userConversationsCount,
        userResourcesCount,
    ] = await Promise.all([
        StatisticsService.getCoachClients(),
        StatisticsService.getPendingClients(),
        StatisticsService.getUserConversations(),
        StatisticsService.getUserResources(),
    ]);

    const stats = [
        {
            name: 'Active Clients',
            value: coachClientsCount.toString(),
            icon: UsersIcon,
        },
        {
            name: 'Pending Clients',
            value: pendingClientsCount.toString(),
            icon: UsersIcon,
        },
        {
            name: 'My Conversations',
            value: userConversationsCount.toString(),
            icon: ChatBubbleLeftRightIcon,
        },
        {
            name: 'My Resources',
            value: userResourcesCount.toString(),
            icon: CalendarIcon,
        },
    ];

    const quickActions = [
        {
            title: 'Find Users',
            description: 'Search for users in your geographical region',
            href: '/app/coach/search',
            icon: UsersIcon,
            color: 'bg-blue-500',
        },
        {
            title: 'My Profile',
            description: 'Update your experience and availability',
            href: '/app/coach/profile',
            icon: CalendarIcon,
            color: 'bg-purple-500',
        },
    ];

    return (
        <div className="space-y-8">
            <div>
                <h1 className="text-3xl font-bold text-gray-900">
                    Coach Dashboard
                </h1>
                <p className="mt-2 text-sm text-gray-600">
                    Welcome back! Manage your clients and coaching sessions.
                </p>
                {enableCoachSection ? (
                    <p>Coach section is enabled.</p>
                ) : (
                    <p>Coach section is disabled.</p>
                )}
            </div>

            {/* Stats */}
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-3">
                {stats.map((stat) => {
                    const Icon = stat.icon;
                    return (
                        <div
                            key={stat.name}
                            className="bg-white overflow-hidden shadow rounded-lg"
                        >
                            <div className="p-5">
                                <div className="flex items-center">
                                    <div className="flex-shrink-0">
                                        <Icon
                                            className="h-6 w-6 text-gray-400"
                                            aria-hidden="true"
                                        />
                                    </div>
                                    <div className="ml-5 w-0 flex-1">
                                        <dl>
                                            <dt className="text-sm font-medium text-gray-500 truncate">
                                                {stat.name}
                                            </dt>
                                            <dd className="text-lg font-semibold text-gray-900">
                                                {stat.value}
                                            </dd>
                                        </dl>
                                    </div>
                                </div>
                            </div>
                        </div>
                    );
                })}
            </div>

            {/* Quick Actions */}
            <div>
                <h2 className="text-xl font-semibold text-gray-900 mb-4">
                    Quick Actions
                </h2>
                <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                    {quickActions.map((action) => {
                        const Icon = action.icon;
                        return (
                            <Link
                                key={action.href}
                                href={action.href}
                                className="relative group bg-white p-6 rounded-lg shadow hover:shadow-lg transition-shadow"
                            >
                                <div>
                                    <span
                                        className={`${action.color} rounded-lg inline-flex p-3 ring-4 ring-white`}
                                    >
                                        <Icon
                                            className="h-6 w-6 text-white"
                                            aria-hidden="true"
                                        />
                                    </span>
                                </div>
                                <div className="mt-4">
                                    <h3 className="text-lg font-medium text-gray-900 group-hover:text-blue-600">
                                        {action.title}
                                    </h3>
                                    <p className="mt-2 text-sm text-gray-500">
                                        {action.description}
                                    </p>
                                </div>
                            </Link>
                        );
                    })}
                </div>
            </div>

            {/* Recent Activity */}
            <div>
                <h2 className="text-xl font-semibold text-gray-900 mb-4">
                    Recent Activity
                </h2>
                <div className="bg-white shadow rounded-lg p-6">
                    <p className="text-sm text-gray-500">
                        No recent activity to display.
                    </p>
                </div>
            </div>
        </div>
    );
}
