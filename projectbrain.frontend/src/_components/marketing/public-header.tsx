'use client';

import Link from 'next/link';
import { useState } from 'react';
import { Dialog, DialogPanel } from '@headlessui/react';
import { Bars3Icon, XMarkIcon } from '@heroicons/react/24/outline';
import { Logo } from './logo';
import { buttonClasses, LOGIN_HREF, SIGNUP_HREF } from './links';

const NAV_LINKS = [
    { href: '/#product', label: 'Product' },
    { href: '/#coaches', label: 'For coaches' },
    { href: '/#who', label: "Who it's for" },
    { href: '/#how', label: 'How it works' },
    { href: '/features', label: 'Features' },
    { href: '/pricing', label: 'Pricing' },
] as const;

export function PublicHeader() {
    const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

    return (
        <header className="sticky top-0 z-40 border-b border-white/10 bg-navy/90 text-white backdrop-blur">
            <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-6 py-3">
                <Link href="/" className="shrink-0">
                    <span className="sr-only">ProjectBrain home</span>
                    <Logo size={40} priority />
                </Link>
                <nav
                    aria-label="Marketing"
                    className="hidden items-center gap-6 text-sm lg:flex"
                >
                    {NAV_LINKS.map((link) => (
                        <Link
                            key={link.href}
                            href={link.href}
                            className="text-white/75 transition-colors hover:text-white"
                        >
                            {link.label}
                        </Link>
                    ))}
                </nav>
                <div className="hidden items-center gap-2 sm:flex">
                    <a
                        href={LOGIN_HREF}
                        className={buttonClasses(
                            'ghost',
                            'text-white hover:bg-white/15'
                        )}
                    >
                        Log in
                    </a>
                    <a href={SIGNUP_HREF} className={buttonClasses('primary')}>
                        Sign up free
                    </a>
                </div>
                <button
                    type="button"
                    onClick={() => setMobileMenuOpen(true)}
                    className="-m-2.5 inline-flex items-center justify-center rounded-md p-2.5 text-white lg:hidden"
                >
                    <span className="sr-only">Open main menu</span>
                    <Bars3Icon aria-hidden="true" className="size-6" />
                </button>
            </div>
            <Dialog
                open={mobileMenuOpen}
                onClose={setMobileMenuOpen}
                className="lg:hidden"
            >
                <div className="fixed inset-0 z-50 bg-navy/40" />
                <DialogPanel className="fixed inset-y-0 right-0 z-50 w-full overflow-y-auto bg-navy px-6 py-4 text-white sm:max-w-sm sm:ring-1 sm:ring-white/10">
                    <div className="flex items-center justify-between">
                        <Link href="/" onClick={() => setMobileMenuOpen(false)}>
                            <span className="sr-only">ProjectBrain home</span>
                            <Logo size={36} />
                        </Link>
                        <button
                            type="button"
                            onClick={() => setMobileMenuOpen(false)}
                            className="-m-2.5 rounded-md p-2.5 text-white hover:bg-white/10"
                        >
                            <span className="sr-only">Close menu</span>
                            <XMarkIcon aria-hidden="true" className="size-6" />
                        </button>
                    </div>
                    <nav aria-label="Marketing" className="mt-8 space-y-1">
                        {NAV_LINKS.map((link) => (
                            <Link
                                key={link.href}
                                href={link.href}
                                onClick={() => setMobileMenuOpen(false)}
                                className="-mx-3 block rounded-lg px-3 py-2 text-base text-white/85 hover:bg-white/10 hover:text-white"
                            >
                                {link.label}
                            </Link>
                        ))}
                    </nav>
                    <div className="mt-8 flex flex-col gap-3 border-t border-white/10 pt-6">
                        <a
                            href={SIGNUP_HREF}
                            className={buttonClasses('primary', 'py-2.5')}
                        >
                            Sign up free
                        </a>
                        <a
                            href={LOGIN_HREF}
                            className={buttonClasses('outline-light', 'py-2.5')}
                        >
                            Log in
                        </a>
                    </div>
                </DialogPanel>
            </Dialog>
        </header>
    );
}
