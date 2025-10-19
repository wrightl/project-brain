import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
    title: 'ProjectBrain - AI-Powered Coaching Platform',
    description:
        'Connect with coaches and access AI-powered support for neurodivergent individuals',
};

export default function RootLayout({
    children,
}: Readonly<{
    children: React.ReactNode;
}>) {
    return (
        <html lang="en">
            <head>
                <link rel="stylesheet" href="https://rsms.me/inter/inter.css" />
            </head>
            <body>{children}</body>
        </html>
    );
}
