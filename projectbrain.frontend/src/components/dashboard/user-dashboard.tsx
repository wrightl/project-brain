import Link from 'next/link';
import {
  ChatBubbleLeftRightIcon,
  DocumentArrowUpIcon,
  ClockIcon,
} from '@heroicons/react/24/outline';

export default function UserDashboard() {
  // TODO: Uncomment after Auth0 API is configured
  // const conversations = await getConversations();
  // const recentConversations = conversations.slice(0, 5);
  const conversations: any[] = [];
  const recentConversations: any[] = [];

  const stats = [
    { name: 'Total Conversations', value: conversations.length.toString(), icon: ChatBubbleLeftRightIcon },
    { name: 'Files Uploaded', value: '0', icon: DocumentArrowUpIcon },
  ];

  const quickActions = [
    {
      title: 'Start New Chat',
      description: 'Begin a new conversation with your AI assistant',
      href: '/user/chat',
      icon: ChatBubbleLeftRightIcon,
      color: 'bg-indigo-500',
    },
    {
      title: 'Upload Files',
      description: 'Upload documents to enhance your AI experience',
      href: '/user/upload',
      icon: DocumentArrowUpIcon,
      color: 'bg-green-500',
    },
  ];

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">My Dashboard</h1>
        <p className="mt-2 text-sm text-gray-600">
          Welcome back! Continue your conversations or start something new.
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
                    <Icon className="h-6 w-6 text-gray-400" aria-hidden="true" />
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
                    <Icon className="h-6 w-6 text-white" aria-hidden="true" />
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
              href="/user/chat"
              className="text-sm font-medium text-indigo-600 hover:text-indigo-500"
            >
              View all
            </Link>
          </div>
          <div className="bg-white shadow rounded-lg overflow-hidden">
            <ul className="divide-y divide-gray-200">
              {recentConversations.map((conversation) => (
                <li key={conversation.id}>
                  <Link
                    href={`/user/chat/${conversation.id}`}
                    className="block hover:bg-gray-50 transition-colors"
                  >
                    <div className="px-6 py-4">
                      <div className="flex items-center justify-between">
                        <div className="flex-1">
                          <p className="text-sm font-medium text-gray-900">
                            {conversation.title}
                          </p>
                          <div className="mt-1 flex items-center text-xs text-gray-500">
                            <ClockIcon className="h-4 w-4 mr-1" />
                            {new Date(conversation.updatedAt).toLocaleDateString()}
                          </div>
                        </div>
                        <div>
                          <ChatBubbleLeftRightIcon className="h-5 w-5 text-gray-400" />
                        </div>
                      </div>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        </div>
      )}
    </div>
  );
}
