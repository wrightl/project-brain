import type { Metadata, Viewport } from 'next';
import './globals.css';

export const metadata: Metadata = {
    title: {
        default: 'ProjectBrain - AI-Powered Coaching Platform',
        template: '%s | ProjectBrain',
    },
    description:
        'Connect with coaches and access AI-powered support for neurodivergent individuals',
    keywords: [
        'AI coaching',
        'neurodivergent support',
        'personalized assistance',
        'mental health',
    ],
    authors: [{ name: 'ProjectBrain' }],
    creator: 'ProjectBrain',
    openGraph: {
        type: 'website',
        locale: 'en_US',
        siteName: 'ProjectBrain',
        title: 'ProjectBrain - AI-Powered Coaching Platform',
        description:
            'Connect with coaches and access AI-powered support for neurodivergent individuals',
    },
};

export const viewport: Viewport = {
    width: 'device-width',
    initialScale: 1,
    themeColor: [
        { media: '(prefers-color-scheme: light)', color: '#ffffff' },
        { media: '(prefers-color-scheme: dark)', color: '#1f2937' },
    ],
};

export default function RootLayout({
    children,
}: Readonly<{
    children: React.ReactNode;
}>) {
    return (
        <html lang="en" suppressHydrationWarning>
            <head>
                <link
                    rel="stylesheet"
                    href="https://rsms.me/inter/inter.css"
                    crossOrigin="anonymous"
                />
            </head>
            <body className="antialiased">{children}</body>
        </html>
    );
}
