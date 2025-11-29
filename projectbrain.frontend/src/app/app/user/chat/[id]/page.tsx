import { RoleGuard } from '@/_components/auth/role-guard';
import { ConversationService } from '@/_services/conversation-service';
import { notFound } from 'next/navigation';
import ChatInterface from '../_components/chat-interface';

interface ChatPageProps {
    params: Promise<{ id: string }>;
}

export default async function ChatPage({ params }: ChatPageProps) {
    const { id } = await params;
    const conversation = await ConversationService.getConversationWithMessages(
        id
    );

    if (!conversation) {
        notFound();
    }

    return (
        <RoleGuard allowedRoles={['user', 'admin']}>
            <div className="h-full w-full flex flex-col bg-gray-50">
                <ChatInterface conversation={conversation} />
            </div>
        </RoleGuard>
    );
}
