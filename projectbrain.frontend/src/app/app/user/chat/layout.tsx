import { Metadata } from 'next';

export const metadata: Metadata = {
    title: 'Chat',
    description: 'Chat with your AI assistant',
};

export default function ChatLayout({
    children,
}: {
    children: React.ReactNode;
}) {
    return (
        <div className="h-[calc(100vh-4rem)] -mx-4 sm:-mx-6 lg:-mx-8 -my-8">
            {children}
        </div>
    );
}
