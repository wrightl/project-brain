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
} from '@heroicons/react/24/outline';

interface DashboardNavProps {
    user: User | null;
    role: UserRole | null;
}

export default function DashboardNav({ user, role }: DashboardNavProps) {
    const pathname = usePathname();

    const adminLinks = [
        { href: '/app/admin', label: 'Dashboard', icon: HomeIcon },
        { href: '/app/admin/users', label: 'Manage Users', icon: UsersIcon },
        {
            href: '/app/admin/upload',
            label: 'Upload Knowledge',
            icon: CloudArrowUpIcon,
        },
    ];

    const coachLinks = [
        { href: '/app/coach', label: 'Dashboard', icon: HomeIcon },
        { href: '/app/coach/search', label: 'Find Users', icon: UsersIcon },
        { href: '/app/coach/settings', label: 'Settings', icon: Cog6ToothIcon },
    ];

    const userLinks = [
        { href: '/app/user', label: 'Dashboard', icon: HomeIcon },
        {
            href: '/app/user/chat',
            label: 'Chat',
            icon: ChatBubbleLeftRightIcon,
        },
        {
            href: '/app/user/upload',
            label: 'Upload Files',
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
                        <div className="text-sm text-gray-700">
                            <span className="font-medium">
                                {user?.fullName || 'User'}
                            </span>
                        </div>
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
