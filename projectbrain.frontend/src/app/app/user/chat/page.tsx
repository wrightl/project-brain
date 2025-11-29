import { RoleGuard } from '@/_components/auth/role-guard';
import ChatInterface from './_components/chat-interface';

export default function ChatPage() {
    return (
        <RoleGuard allowedRoles={['user', 'admin']}>
            <div className="h-full w-full flex flex-col bg-gray-50">
                <ChatInterface />
            </div>
        </RoleGuard>
    );
}
