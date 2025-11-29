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

interface DashboardNavProps {
    user: User | null;
    role: UserRole | null;
}

export default function DashboardNav({ user, role }: DashboardNavProps) {
    const pathname = usePathname();

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
    ];

    const userLinks = [
        { href: '/app/user', label: 'Dashboard', icon: HomeIcon },
        {
            href: '/app/user/chat',
            label: 'Chat',
            icon: ChatBubbleLeftRightIcon,
        },
        {
            href: '/app/user/manage-files',
            label: 'Manage Files',
            icon: CloudArrowUpIcon,
        },
        {
            href: '/app/user/voicenotes',
            label: 'Voice Notes',
            icon: MusicalNoteIcon,
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
                                return (
                                    <Link
                                        key={link.href}
                                        href={link.href}
                                        className={`inline-flex items-center px-3 py-2 text-sm font-medium rounded-md ${
                                            isActive
                                                ? 'text-indigo-600 bg-indigo-50'
                                                : 'text-gray-700 hover:text-indigo-600 hover:bg-gray-50'
                                        }`}
                                    >
                                        <Icon className="h-5 w-5 mr-2" />
                                        {link.label}
                                    </Link>
                                );
                            })}
                        </div>
                    </div>
                    <div className="flex items-center space-x-4">
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
