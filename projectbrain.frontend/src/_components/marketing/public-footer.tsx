import Link from 'next/link';
import { Logo } from './logo';
import { LOGIN_HREF, SIGNUP_HREF } from './links';

export function PublicFooter() {
    const year = new Date().getFullYear();
    return (
        <footer className="border-t border-white/10 bg-navy px-6 py-12 text-white">
            <div className="mx-auto flex max-w-6xl flex-col gap-8 md:flex-row md:items-start md:justify-between">
                <div>
                    <Logo size={36} />
                    <p className="mt-3 max-w-xs text-sm text-white/60">
                        An AI coaching buddy and real coaches for
                        neurodivergent entrepreneurs — goals, journal, and
                        support in one calm place.
                    </p>
                </div>
                <div className="grid grid-cols-2 gap-8 text-sm sm:grid-cols-3">
                    <div>
                        <p className="font-medium text-white">Product</p>
                        <ul className="mt-3 space-y-2 text-white/60">
                            <li>
                                <Link href="/#product" className="hover:text-white">
                                    Overview
                                </Link>
                            </li>
                            <li>
                                <Link href="/features" className="hover:text-white">
                                    Features
                                </Link>
                            </li>
                            <li>
                                <Link href="/pricing" className="hover:text-white">
                                    Pricing
                                </Link>
                            </li>
                        </ul>
                    </div>
                    <div>
                        <p className="font-medium text-white">Get started</p>
                        <ul className="mt-3 space-y-2 text-white/60">
                            <li>
                                <a href={SIGNUP_HREF} className="hover:text-white">
                                    Sign up
                                </a>
                            </li>
                            <li>
                                <a href={LOGIN_HREF} className="hover:text-white">
                                    Log in
                                </a>
                            </li>
                            <li>
                                <Link href="/#coaches" className="hover:text-white">
                                    For coaches
                                </Link>
                            </li>
                        </ul>
                    </div>
                    <div>
                        <p className="font-medium text-white">Company</p>
                        <ul className="mt-3 space-y-2 text-white/60">
                            <li>
                                <a
                                    href="https://www.dotanddashconsulting.com"
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="hover:text-white"
                                >
                                    Dot + Dash Consulting
                                </a>
                            </li>
                            <li>
                                <a
                                    href="https://alfa.dotanddashconsulting.com"
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="hover:text-white"
                                >
                                    Alfa bookkeeping
                                </a>
                            </li>
                        </ul>
                    </div>
                </div>
            </div>
            <div className="mx-auto mt-10 max-w-6xl border-t border-white/10 pt-6 text-center text-xs text-white/40">
                <p>© {year} Dot and Dash Consulting. All rights reserved.</p>
            </div>
        </footer>
    );
}
