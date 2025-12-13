import type { Metadata, Viewport } from 'next';
import './globals.css';
import { Toaster } from 'react-hot-toast';
import { Providers } from '@/_components/providers';
import { ErrorBoundary } from '@/_components/error-boundary';
import { SkipLink } from '@/_components/skip-link';
import { WebVitalsScript } from '@/_components/web-vitals-script';

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
            <body className="antialiased">
                <SkipLink />
                <WebVitalsScript />
                <ErrorBoundary>
                    <Providers>
                        <main id="main-content">
                            {children}
                        </main>
                    </Providers>
                </ErrorBoundary>
                <Toaster
                    position="top-right"
                    toastOptions={{
                        duration: 4000,
                        style: {
                            background: '#363636',
                            color: '#fff',
                        },
                        success: {
                            duration: 3000,
                            iconTheme: {
                                primary: '#10b981',
                                secondary: '#fff',
                            },
                        },
                        error: {
                            duration: 4000,
                            iconTheme: {
                                primary: '#ef4444',
                                secondary: '#fff',
                            },
                        },
                    }}
                />
            </body>
        </html>
    );
}
