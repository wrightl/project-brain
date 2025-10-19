import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
    title: 'Project Brain',
    description: 'Welcome to Project Brain',
};

export default function RootLayout({
    children,
}: Readonly<{
    children: React.ReactNode;
}>) {
    return (
        <html lang="en">
            {/* <!-- get this working --> */}
            {/* <script>
                document.documentElement.classList.toggle( 'dark',
                localStorage.theme === 'dark' || (!('theme' in localStorage) &&
                window.matchMedia('(prefers-color-scheme: dark)').matches) )
            </script> */}
            <head>
                <link rel="stylesheet" href="https://rsms.me/inter/inter.css" />
            </head>
            <body>
                <div className="page-layout__content">{children}</div>
            </body>
        </html>
    );
}
