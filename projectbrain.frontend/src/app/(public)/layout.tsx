import { Outfit } from 'next/font/google';
import { PublicHeader } from '@/_components/marketing/public-header';
import { PublicFooter } from '@/_components/marketing/public-footer';

// Same display face as the Alfa marketing site, so the Dot + Dash products read as a family.
const outfit = Outfit({
    subsets: ['latin'],
    variable: '--font-outfit',
    display: 'swap',
});

export default function PublicLayout({
    children,
}: Readonly<{
    children: React.ReactNode;
}>) {
    return (
        <div className={`${outfit.variable} flex min-h-screen flex-col bg-navy font-display text-navy`}>
            <PublicHeader />
            <div className="flex flex-1 flex-col">{children}</div>
            <PublicFooter />
        </div>
    );
}
