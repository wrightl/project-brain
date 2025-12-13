import dynamicImport from 'next/dynamic';
import { RoleGuard } from '@/_components/auth/role-guard';
import { SkeletonCard } from '@/_components/ui/skeleton';

const ChatInterface = dynamicImport(() => import('./_components/chat-interface'), {
    loading: () => (
        <div className="h-full w-full flex flex-col bg-gray-50 p-8">
            <SkeletonCard />
        </div>
    ),
});

export default function ChatPage() {
    return (
        <RoleGuard allowedRoles={['user', 'admin']}>
            <div className="h-full w-full flex flex-col bg-gray-50">
                <ChatInterface />
            </div>
        </RoleGuard>
    );
}
