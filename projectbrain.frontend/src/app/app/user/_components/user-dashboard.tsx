import Link from 'next/link';
import {
    ChatBubbleLeftRightIcon,
    DocumentArrowUpIcon,
    UserGroupIcon,
} from '@heroicons/react/24/outline';
import { ChatService } from '@/_services/chat-service';
import { ResourceService } from '@/_services/resource-service';
import { StatisticsService } from '@/_services/statistics-service';
import { ConversationListItem } from './conversation-list-item';

export default async function UserDashboard() {
    const conversations = await ChatService.getConversations();
    const resources = await ResourceService.getResources();
    const [userConversationsCount, userResourcesCount] = await Promise.all([
        StatisticsService.getUserConversations(),
        StatisticsService.getUserResources(),
    ]);
    const recentConversations = conversations.slice(0, 5);

    const stats = [
        {
            name: 'Total Conversations',
            value: userConversationsCount.toString(),
            icon: ChatBubbleLeftRightIcon,
        },
        {
            name: 'Files Uploaded',
            value: userResourcesCount.toString(),
            icon: DocumentArrowUpIcon,
        },
    ];

    const quickActions = [
        {
            title: 'Start New Chat',
            description: 'Begin a new conversation with your AI assistant',
            href: '/app/user/chat',
            icon: ChatBubbleLeftRightIcon,
            color: 'bg-indigo-500',
        },
        {
            title: 'Find Coaches',
            description:
                'Search for coaches by location, age groups, and specialisms',
            href: '/app/user/find-coaches',
            icon: UserGroupIcon,
            color: 'bg-purple-500',
        },
        {
            title: 'Manage Files',
            description: 'Manage documents to enhance your AI experience',
            href: '/app/user/manage-files',
            icon: DocumentArrowUpIcon,
            color: 'bg-green-500',
        },
    ];

    return (
        <div className="space-y-8">
            <div>
                <h1 className="text-3xl font-bold text-gray-900">
                    My Dashboard
                </h1>
                <p className="mt-2 text-sm text-gray-600">
                    Welcome back! Continue your conversations or start something
                    new.
                </p>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
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
                <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
                    {quickActions.map((action) => {
                        const Icon = action.icon;
                        return (
                            <Link
                                key={action.href}
                                href={action.href || ''}
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
                                    <h3 className="text-lg font-medium text-gray-900 group-hover:text-indigo-600">
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

            {/* Recent Conversations */}
            {recentConversations.length > 0 && (
                <div>
                    <div className="flex items-center justify-between mb-4">
                        <h2 className="text-xl font-semibold text-gray-900">
                            Recent Conversations
                        </h2>
                        <Link
                            href="/app/user/chat"
                            className="text-sm font-medium text-indigo-600 hover:text-indigo-500"
                        >
                            View all
                        </Link>
                    </div>
                    <div className="bg-white shadow rounded-lg overflow-hidden">
                        <ul className="divide-y divide-gray-200">
                            {recentConversations.map((conversation) => (
                                <ConversationListItem
                                    key={conversation.id}
                                    conversation={conversation}
                                />
                            ))}
                        </ul>
                    </div>
                </div>
            )}
        </div>
    );
}
