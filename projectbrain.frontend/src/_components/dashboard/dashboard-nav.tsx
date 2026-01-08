'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useState, useEffect, useRef } from 'react';
import { User, UserRole } from '@/_lib/types';
import {
    HomeIcon,
    ChatBubbleLeftRightIcon,
    SparklesIcon,
    UsersIcon,
    CloudArrowUpIcon,
    ArrowRightStartOnRectangleIcon,
    UserIcon,
    DocumentTextIcon,
    AcademicCapIcon,
    ChevronDownIcon,
    CogIcon,
    CreditCardIcon,
    Bars3Icon,
    XMarkIcon,
} from '@heroicons/react/24/outline';
import AvailabilityStatusDropdown from './availability-status-dropdown';
import { useUnreadMessagesCount } from '@/_hooks/use-unread-messages-count';

interface DashboardNavProps {
    user: User | null;
    role: UserRole | null;
}

function UserProfileDropdown({
    user,
    role,
}: {
    user: User | null;
    role: UserRole | null;
}) {
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (
                dropdownRef.current &&
                !dropdownRef.current.contains(event.target as Node)
            ) {
                setIsOpen(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, []);

    const menuItems = [];

    if (role === 'user') {
        menuItems.push({
            href: '/app/user/profile',
            label: 'Profile',
            icon: UserIcon,
        });
        menuItems.push({
            href: '/app/user/resources',
            label: 'Resources',
            icon: CloudArrowUpIcon,
        });
        menuItems.push({
            href: '/app/user/subscription',
            label: 'Subscription',
            icon: CreditCardIcon,
        });
        menuItems.push({
            href: '/app/user/preferences',
            label: 'Preferences',
            icon: CogIcon,
        });
        menuItems.push({
            href: '/auth/logout',
            label: 'Logout',
            icon: ArrowRightStartOnRectangleIcon,
        });
    } else if (role === 'coach') {
        menuItems.push({
            href: '/app/coach/profile',
            label: 'Profile',
            icon: UserIcon,
        });
        menuItems.push({
            href: '/auth/logout',
            label: 'Logout',
            icon: ArrowRightStartOnRectangleIcon,
        });
    } else if (role === 'admin') {
        menuItems.push({
            href: '/auth/logout',
            label: 'Logout',
            icon: ArrowRightStartOnRectangleIcon,
        });
    }

    return (
        <div className="relative" ref={dropdownRef}>
            <button
                onClick={() => setIsOpen(!isOpen)}
                className="inline-flex items-center px-3 py-2 text-sm font-medium text-gray-700 hover:text-gray-900 hover:bg-gray-50 rounded-md"
            >
                <UserIcon className="h-5 w-5 mr-2" />
                {role === 'admin'
                    ? user?.fullName || 'User'
                    : user?.firstName || 'User'}
                <ChevronDownIcon className="h-4 w-4 ml-2" />
            </button>

            {isOpen && (
                <div className="absolute right-0 mt-2 w-56 bg-white rounded-md shadow-lg z-50 border border-gray-200">
                    <div className="py-1">
                        {menuItems.map((item) => {
                            const Icon = item.icon;
                            return (
                                <Link
                                    key={item.href}
                                    href={item.href}
                                    onClick={() => setIsOpen(false)}
                                    className="flex items-center px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                                >
                                    <Icon className="h-5 w-5 mr-3" />
                                    {item.label}
                                </Link>
                            );
                        })}
                    </div>
                </div>
            )}
        </div>
    );
}

export default function DashboardNav({ user, role }: DashboardNavProps) {
    const pathname = usePathname();
    const { unreadCount } = useUnreadMessagesCount();
    const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
    const mobileMenuRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (
                mobileMenuRef.current &&
                !mobileMenuRef.current.contains(event.target as Node)
            ) {
                setMobileMenuOpen(false);
            }
        };

        if (mobileMenuOpen) {
            document.addEventListener('mousedown', handleClickOutside);
        }

        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [mobileMenuOpen]);

    const adminLinks = [
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
        { href: '/app/coach/clients', label: 'Clients', icon: UsersIcon },
        { href: '/app/coach/search', label: 'Find Users', icon: UsersIcon },
        {
            href: '/app/coach/messages',
            label: 'Messaging',
            icon: ChatBubbleLeftRightIcon,
            showUnreadBadge: true,
        },
    ];

    const userLinks = [
        {
            href: '/app/user/chat',
            label: 'Chat',
            icon: SparklesIcon,
        },
        {
            href: '/app/user/eggs',
            label: 'Eggs',
            icon: AcademicCapIcon,
        },
        {
            href: '/app/user/connections',
            label: 'Network',
            icon: UsersIcon,
        },
    ];

    const links =
        role === 'admin'
            ? adminLinks
            : role === 'coach'
            ? coachLinks
            : userLinks;

    const userMenuItems = [
        {
            href: '/app/user/profile',
            label: 'Profile',
            icon: UserIcon,
        },
        {
            href: '/app/user/resources',
            label: 'Resources',
            icon: CloudArrowUpIcon,
        },
        {
            href: '/app/user/subscription',
            label: 'Subscription',
            icon: CreditCardIcon,
        },
        {
            href: '/app/user/preferences',
            label: 'Preferences',
            icon: CogIcon,
        },
        {
            href: '/auth/logout',
            label: 'Logout',
            icon: ArrowRightStartOnRectangleIcon,
        },
    ];

    const coachMenuItems = [
        {
            href: '/app/coach/profile',
            label: 'Profile',
            icon: UserIcon,
        },
        {
            href: '/auth/logout',
            label: 'Logout',
            icon: ArrowRightStartOnRectangleIcon,
        },
    ];

    const adminMenuItems = [
        {
            href: '/auth/logout',
            label: 'Logout',
            icon: ArrowRightStartOnRectangleIcon,
        },
    ];

    const accountMenuItems =
        role === 'admin'
            ? adminMenuItems
            : role === 'coach'
            ? coachMenuItems
            : userMenuItems;

    return (
        <nav className="bg-white shadow-sm border-b border-gray-200">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                <div className="flex justify-between h-16">
                    <div className="flex space-x-8">
                        <div className="flex items-center">
                            <Link
                                href={`/app/${role?.toLowerCase()}`}
                                className="flex items-center text-xl font-bold text-indigo-600 hover:text-indigo-700"
                            >
                                <HomeIcon className="h-5 w-5 mr-2" />
                                ProjectBrain
                            </Link>
                        </div>
                        <div className="hidden sm:flex sm:space-x-4">
                            {links.map((link) => {
                                const Icon = link.icon;
                                const isActive = pathname === link.href;
                                const showBadge =
                                    (link as any).showUnreadBadge &&
                                    unreadCount > 0;
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
                                                {unreadCount > 99
                                                    ? '99+'
                                                    : unreadCount}
                                            </span>
                                        )}
                                    </Link>
                                );
                            })}
                        </div>
                    </div>
                    <div className="flex items-center space-x-4">
                        {/* Mobile community link - visible on mobile for users only */}
                        {role === 'user' && (
                            <a
                                href="https://www.skool.com/neurodivergent-entrepreneurs-8159"
                                target="_blank"
                                rel="noopener noreferrer"
                                className="sm:hidden inline-flex items-center justify-center p-2 rounded-md text-gray-700 hover:text-gray-900 hover:bg-gray-50"
                                title="Neurodivergent Entrepreneurs Community"
                            >
                                <img
                                    src="https://assets.skool.com/f/31ad136c883e458d9b6f81be08dc9b93/658fb61b21394130bea6407b1fd1dee2916ed48976e4409ca9ed76946f1e4a6f"
                                    alt="Community"
                                    className="h-8 w-8 rounded-md"
                                />
                            </a>
                        )}
                        {/* Mobile menu button - show for all roles */}
                        <div className="sm:hidden relative" ref={mobileMenuRef}>
                            <button
                                onClick={() =>
                                    setMobileMenuOpen(!mobileMenuOpen)
                                }
                                className="inline-flex items-center justify-center p-2 rounded-md text-gray-700 hover:text-gray-900 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-indigo-500"
                                aria-expanded="false"
                            >
                                <span className="sr-only">Open main menu</span>
                                {mobileMenuOpen ? (
                                    <XMarkIcon className="block h-6 w-6" />
                                ) : (
                                    <Bars3Icon className="block h-6 w-6" />
                                )}
                            </button>

                            {mobileMenuOpen && (
                                <div className="absolute right-0 mt-2 w-64 bg-white rounded-md shadow-lg z-50 border border-gray-200">
                                    <div className="py-1">
                                        {/* Navigation Links */}
                                        <div className="px-4 py-2 border-b border-gray-200">
                                            <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
                                                Navigation
                                            </p>
                                        </div>
                                        {links.map((link) => {
                                            const Icon = link.icon;
                                            const isActive =
                                                pathname === link.href;
                                            const showBadge =
                                                (link as any).showUnreadBadge &&
                                                unreadCount > 0;
                                            return (
                                                <Link
                                                    key={link.href}
                                                    href={link.href}
                                                    onClick={() =>
                                                        setMobileMenuOpen(false)
                                                    }
                                                    className={`relative flex items-center px-4 py-2 text-sm ${
                                                        isActive
                                                            ? 'bg-indigo-50 text-indigo-700'
                                                            : 'text-gray-700 hover:bg-gray-50'
                                                    }`}
                                                >
                                                    <Icon className="h-5 w-5 mr-3" />
                                                    {link.label}
                                                    {showBadge && (
                                                        <span className="absolute right-4 inline-flex items-center justify-center px-2 py-0.5 rounded-full text-xs font-medium bg-indigo-600 text-white">
                                                            {unreadCount > 99
                                                                ? '99+'
                                                                : unreadCount}
                                                        </span>
                                                    )}
                                                </Link>
                                            );
                                        })}
                                        {/* Community Link - only for users */}
                                        {/* {role === 'user' && (
                                            <>
                                                <div className="px-4 py-2 border-t border-gray-200 mt-1">
                                                    <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
                                                        Community
                                                    </p>
                                                </div>
                                                <a
                                                    href="https://www.skool.com/neurodivergent-entrepreneurs-8159"
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    onClick={() =>
                                                        setMobileMenuOpen(false)
                                                    }
                                                    className="flex items-center px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                                                >
                                                    <img
                                                        src="https://assets.skool.com/f/31ad136c883e458d9b6f81be08dc9b93/658fb61b21394130bea6407b1fd1dee2916ed48976e4409ca9ed76946f1e4a6f"
                                                        alt="Community"
                                                        className="h-10 w-10 mr-3 rounded-md"
                                                    />
                                                    Community
                                                </a>
                                            </>
                                        )} */}
                                        {/* Account Menu Items */}
                                        <div className="px-4 py-2 border-t border-gray-200 mt-1">
                                            <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
                                                Account
                                            </p>
                                        </div>
                                        {accountMenuItems.map((item) => {
                                            const Icon = item.icon;
                                            return (
                                                <Link
                                                    key={item.href}
                                                    href={item.href}
                                                    onClick={() =>
                                                        setMobileMenuOpen(false)
                                                    }
                                                    className="flex items-center px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                                                >
                                                    <Icon className="h-5 w-5 mr-3" />
                                                    {item.label}
                                                </Link>
                                            );
                                        })}
                                    </div>
                                </div>
                            )}
                        </div>

                        {/* Desktop view */}
                        <div className="hidden sm:flex items-center space-x-4">
                            {role === 'coach' && <AvailabilityStatusDropdown />}
                            {role === 'user' && (
                                <a
                                    href="https://www.skool.com/neurodivergent-entrepreneurs-8159"
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="inline-flex items-center px-3 py-2 text-sm font-medium text-gray-700 hover:text-gray-900 hover:bg-gray-50 rounded-md"
                                    title="Skool - Neurodivergent Entrepreneurs Community"
                                >
                                    <img
                                        src="https://assets.skool.com/f/31ad136c883e458d9b6f81be08dc9b93/658fb61b21394130bea6407b1fd1dee2916ed48976e4409ca9ed76946f1e4a6f"
                                        alt="Community"
                                        className="h-10 w-10 rounded-md"
                                    />
                                </a>
                            )}
                            {role !== 'admin' && (
                                <UserProfileDropdown user={user} role={role} />
                            )}
                            {role === 'admin' && (
                                <UserProfileDropdown user={user} role={role} />
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </nav>
    );
}
