'use client';

import { useState, useMemo } from 'react';

import {
    WalletIcon,
    UserCircleIcon,
    Cog6ToothIcon,
    ChevronDownIcon,
    AcademicCapIcon,
} from '@heroicons/react/24/outline';

import { usePathname, useRouter } from 'next/navigation';
import Link from 'next/link';

export default function SidebarNav() {
    const items: ISidebarItem[] = [
        {
            name: 'Dashboard',
            path: '/app',
            icon: UserCircleIcon,
        },
        {
            name: 'Eggs',
            path: '/app/user/eggs',
            icon: AcademicCapIcon,
        },
        {
            name: 'Payment',
            path: '/payment',
            icon: WalletIcon,
        },
        {
            name: 'Accounts',
            path: '/accounts',
            icon: UserCircleIcon,
        },
        {
            name: 'Settings',
            path: '/settings',
            icon: Cog6ToothIcon,
            items: [
                {
                    name: 'General',
                    path: '/settings',
                },
                {
                    name: 'Security',
                    path: '/settings/security',
                },
                {
                    name: 'Notifications',
                    path: '/settings/notifications',
                },
            ],
        },
    ];

    return (
        <div className="fixed top-0 left-0 h-screen w-64 bg-white shadow-lg z-10 p-4">
            <div className="flex flex-col space-y-10 w-full">
                <Link href="/">
                    <img className="h-10 w-fit" src="/dotdash.png" alt="Logo" />
                </Link>
                <div className="flex flex-col space-y-2">
                    {items.map((item, index) => (
                        <SidebarNavItem key={index} item={item} />
                    ))}
                </div>
            </div>
        </div>
    );
}

interface ISidebarItem {
    name: string;
    path: string;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    icon: any;
    items?: ISubItem[];
}

interface ISubItem {
    name: string;
    path: string;
}

const SidebarNavItem = ({ item }: { item: ISidebarItem }) => {
    const { name, icon: Icon, items, path } = item;
    const [expanded, setExpanded] = useState(false);
    const router = useRouter();
    const pathname = usePathname();

    const onClick = () => {
        if (items && items.length > 0) {
            return setExpanded(!expanded);
        }

        return router.push(path);
    };
    const isActive = useMemo(() => {
        if (items && items.length > 0) {
            if (items.find((item) => item.path === pathname)) {
                setExpanded(true);
                return true;
            }
        }

        return path === pathname;
    }, [items, path, pathname]);

    return (
        <>
            <div
                className={`flex items-center p-3 rounded-lg hover:bg-sidebar-background cursor-pointer hover:text-sidebar-active justify-between
       ${isActive && 'text-sidebar-active bg-sidebar-background'}
      `}
                onClick={onClick}
            >
                <div className="flex items-center space-x-2">
                    <Icon size={20} />
                    <p className="text-sm font-semibold">{name} </p>
                </div>
                {items && items.length > 0 && (
                    <ChevronDownIcon className="size-6" />
                )}
            </div>
            {expanded && items && items.length > 0 && (
                <div className="flex flex-col space-y-1 ml-10">
                    {items.map((item) => (
                        <SidebarNavSubItem key={item.path} item={item} />
                    ))}
                </div>
            )}
        </>
    );
};

interface ISubItem {
    name: string;
    path: string;
}

const SidebarNavSubItem = ({ item }: { item: ISubItem }) => {
    const { name, path } = item;
    const router = useRouter();
    const pathname = usePathname();

    const onClick = () => {
        router.push(path);
    };

    const isActive = useMemo(() => path === pathname, [path, pathname]);

    return (
        <div
            className={`text-sm hover:text-sidebar-active hover:font-semibold cursor-pointer ${
                isActive ? 'text-sidebar-active font-semibold' : ''
            }`}
            onClick={onClick}
        >
            {name}
        </div>
    );
};
