import { RoleGuard } from '@/_components/auth/role-guard';
import { ConversationService } from '@/_services/conversation-service';
import { getSession } from '@/_lib/auth';
import { notFound } from 'next/navigation';
import ChatInterface from '../../_components/chat-interface';

interface StrategiesChatPageProps {
    params: Promise<{ id: string }>;
}

export default async function StrategiesChatPage({
    params,
}: StrategiesChatPageProps) {
    const { id } = await params;

    const conversation = await ConversationService.getConversationWithMessages(
        id
    );
    if (!conversation) {
        notFound();
    }

    const session = await getSession();
    const displayName =
        session?.user?.name ??
        session?.user?.nickname ??
        session?.user?.email ??
        'there';

    return (
        <RoleGuard allowedRoles={['user', 'admin']}>
            <div className="h-full w-full flex flex-col bg-gray-50">
                <ChatInterface
                    conversation={conversation}
                    mode="strategies"
                    displayName={displayName}
                />
            </div>
        </RoleGuard>
    );
}

