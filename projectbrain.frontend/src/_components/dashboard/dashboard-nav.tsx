'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { User, UserRole } from '@/_lib/types';
import {
    HomeIcon,
    ChatBubbleLeftRightIcon,
    UsersIcon,
    CloudArrowUpIcon,
    Cog6ToothIcon,
    ArrowRightStartOnRectangleIcon,
    UserIcon,
    DocumentTextIcon,
    MusicalNoteIcon,
} from '@heroicons/react/24/outline';
import AvailabilityStatusDropdown from './availability-status-dropdown';
import { useUnreadMessagesCount } from '@/_hooks/use-unread-messages-count';

interface DashboardNavProps {
    user: User | null;
    role: UserRole | null;
}

export default function DashboardNav({ user, role }: DashboardNavProps) {
    const pathname = usePathname();
    const { unreadCount } = useUnreadMessagesCount();

    const adminLinks = [
        { href: '/app/admin', label: 'Dashboard', icon: HomeIcon },
        { href: '/app/admin/users', label: 'Users', icon: UsersIcon },
        {
            href: '/app/admin/manage-files',
            label: 'Knowledge',
            icon: CloudArrowUpIcon,
        },
        {
            href: '/app/admin/quizzes',
            label: 'Quizzes',
            icon: DocumentTextIcon,
        },
    ];

    const coachLinks = [
        { href: '/app/coach', label: 'Dashboard', icon: HomeIcon },
        { href: '/app/coach/clients', label: 'My Clients', icon: UsersIcon },
        { href: '/app/coach/search', label: 'Find Users', icon: UsersIcon },
        { href: '/app/coach/messages', label: 'Messaging', icon: ChatBubbleLeftRightIcon, showUnreadBadge: true },
    ];

    const userLinks = [
        { href: '/app/user', label: 'Dashboard', icon: HomeIcon },
        {
            href: '/app/user/chat',
            label: 'Chat',
            icon: ChatBubbleLeftRightIcon,
        },
        {
            href: '/app/user/connections',
            label: 'My Network',
            icon: UsersIcon,
        },
        {
            href: '/app/user/resources',
            label: 'My Resources',
            icon: CloudArrowUpIcon,
        },
    ];

    const links =
        role === 'admin'
            ? adminLinks
            : role === 'coach'
            ? coachLinks
            : userLinks;

    return (
        <nav className="bg-white shadow-sm border-b border-gray-200">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                <div className="flex justify-between h-16">
                    <div className="flex space-x-8">
                        <div className="flex items-center">
                            <span className="text-xl font-bold text-indigo-600">
                                ProjectBrain
                            </span>
                        </div>
                        <div className="hidden sm:flex sm:space-x-4">
                            {links.map((link) => {
                                const Icon = link.icon;
                                const isActive = pathname === link.href;
                                const showBadge = (link as any).showUnreadBadge && unreadCount > 0;
                                return (
                                    <Link
                                        key={link.href}
                                        href={link.href}
                                        className={`relative inline-flex items-center px-3 py-2 text-sm font-medium rounded-md ${
                                            isActive
                                                ? 'text-indigo-600 bg-indigo-50'
                                                : 'text-gray-700 hover:text-indigo-600 hover:bg-gray-50'
                                        }`}
                                    >
                                        <Icon className="h-5 w-5 mr-2" />
                                        {link.label}
                                        {showBadge && (
                                            <span className="absolute -top-1 -right-1 inline-flex items-center justify-center px-2 py-0.5 rounded-full text-xs font-medium bg-indigo-600 text-white">
                                                {unreadCount > 99 ? '99+' : unreadCount}
                                            </span>
                                        )}
                                    </Link>
                                );
                            })}
                        </div>
                    </div>
                    <div className="flex items-center space-x-4">
                        {role === 'coach' && <AvailabilityStatusDropdown />}
                        {role === 'admin' && (
                            <span className="inline-flex items-center px-3 py-2 text-sm font-medium text-gray-700 rounded-md">
                                <UserIcon className="h-5 w-5 mr-2" />
                                {user?.fullName || 'User'}
                            </span>
                        )}
                        {role !== 'admin' && (
                            <Link
                                href={
                                    role === 'coach'
                                        ? '/app/coach/profile'
                                        : '/app/user/profile'
                                }
                                className="inline-flex items-center px-3 py-2 text-sm font-medium text-gray-700 hover:text-gray-900 hover:bg-gray-50 rounded-md"
                            >
                                <UserIcon className="h-5 w-5 mr-2" />
                                {user?.fullName || 'User'}
                            </Link>
                        )}

                        <a
                            href="/auth/logout"
                            className="inline-flex items-center px-3 py-2 text-sm font-medium text-gray-700 hover:text-gray-900 hover:bg-gray-50 rounded-md"
                        >
                            <ArrowRightStartOnRectangleIcon className="h-5 w-5 mr-2" />
                            Logout
                        </a>
                    </div>
                </div>
            </div>
        </nav>
    );
}
