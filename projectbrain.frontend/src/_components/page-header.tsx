'use client';

import Image from 'next/image';
import Link from 'next/link';
import { useState } from 'react';
import { Dialog, DialogPanel } from '@headlessui/react';
import { Bars3Icon, XMarkIcon } from '@heroicons/react/24/outline';
import { LoginButton } from './buttons/login-button';
import { MobileLoginButton } from './buttons/mobile-login-button';

const navigation = [
    { name: 'Product', href: '/product' },
    { name: 'Pricing', href: '/pricing' },
    { name: 'Features', href: '/features' },
    { name: 'Company', href: 'https://dotanddashconsulting.com' },
];

export default function PageHeader() {
    const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

    return (
        <header className="fixed w-full z-50">
            <nav
                aria-label="Global"
                className="bg-blue-300 border-gray-200 dark:bg-gray-900 flex items-center justify-between p-6 lg:px-8"
            >
                <div className="flex lg:flex-1">
                    <Link href="/" className="-m-1.5 p-1.5">
                        <span className="sr-only">Your Company</span>
                        <Image
                            alt=""
                            width={60}
                            height={60}
                            src="/dotdash.png"
                            className="h-8 w-auto"
                        />
                    </Link>
                </div>
                <div className="flex lg:hidden">
                    <button
                        type="button"
                        onClick={() => setMobileMenuOpen(true)}
                        className="-m-2.5 inline-flex items-center justify-center rounded-md p-2.5 text-gray-700"
                    >
                        <span className="sr-only">Open main menu</span>
                        <Bars3Icon aria-hidden="true" className="size-6" />
                    </button>
                </div>
                <div className="hidden lg:flex lg:gap-x-12">
                    {navigation.map((item) => {
                        const isExternal = item.href.startsWith('http');
                        if (isExternal) {
                            return (
                                <a
                                    key={item.name}
                                    href={item.href}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="text-sm/6 font-semibold text-fuchsia-300 dark:text-white"
                                >
                                    {item.name}
                                </a>
                            );
                        }
                        return (
                            <Link
                                key={item.name}
                                href={item.href}
                                className="text-sm/6 font-semibold text-[rgb(245,152,255)] dark:text-white"
                            >
                                {item.name}
                            </Link>
                        );
                    })}
                </div>
                <div className="hidden lg:flex lg:flex-1 lg:justify-end">
                    <LoginButton />
                </div>
            </nav>
            <Dialog
                open={mobileMenuOpen}
                onClose={setMobileMenuOpen}
                className="lg:hidden"
            >
                <div className="fixed inset-0 z-50" />
                <DialogPanel className="fixed inset-y-0 right-0 z-50 w-full overflow-y-auto bg-white px-6 py-6 sm:max-w-sm sm:ring-1 sm:ring-gray-900/10 dark:bg-gray-900">
                    <div className="flex items-center justify-between">
                        <Link href="/" className="-m-1.5 p-1.5">
                            <span className="sr-only">Your Company</span>
                            <Image
                                alt=""
                                width={60}
                                height={60}
                                src="/dotdash.png"
                                className="h-8 w-auto"
                            />
                        </Link>
                        <button
                            type="button"
                            onClick={() => setMobileMenuOpen(false)}
                            className="-m-2.5 rounded-md p-2.5 text-gray-700 dark:text-white hover:bg-gray-50 dark:hover:text-gray-900 "
                        >
                            <span className="sr-only">Close menu</span>
                            <XMarkIcon aria-hidden="true" className="size-6" />
                        </button>
                    </div>
                    <div className="mt-6 flow-root">
                        <div className="-my-6 divide-y divide-gray-500/10">
                            <div className="space-y-2 py-6">
                                {navigation.map((item) => {
                                    const isExternal =
                                        item.href.startsWith('http');
                                    if (isExternal) {
                                        return (
                                            <a
                                                key={item.name}
                                                href={item.href}
                                                target="_blank"
                                                rel="noopener noreferrer"
                                                className="-mx-3 block rounded-lg px-3 py-2 text-base/7 font-semibold text-fuchsia-300 hover:bg-gray-50 dark:hover:text-gray-900 dark:text-white"
                                                onClick={() =>
                                                    setMobileMenuOpen(false)
                                                }
                                            >
                                                {item.name}
                                            </a>
                                        );
                                    }
                                    return (
                                        <Link
                                            key={item.name}
                                            href={item.href}
                                            className="-mx-3 block rounded-lg px-3 py-2 text-base/7 font-semibold text-[rgb(245,152,255)] hover:bg-gray-50 dark:hover:text-gray-900 dark:text-white"
                                            onClick={() =>
                                                setMobileMenuOpen(false)
                                            }
                                        >
                                            {item.name}
                                        </Link>
                                    );
                                })}
                            </div>
                            <div className="py-6">
                                <MobileLoginButton />
                            </div>
                        </div>
                    </div>
                </DialogPanel>
            </Dialog>
        </header>
    );
}
