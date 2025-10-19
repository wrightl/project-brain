import { RoleGuard } from '@/components/auth/role-guard';
import { getConversationWithMessages } from '@/lib/api-client';
import ChatInterface from '@/components/chat/chat-interface';
import { notFound } from 'next/navigation';

interface ChatPageProps {
  params: Promise<{ id: string }>;
}

export default async function ChatPage({ params }: ChatPageProps) {
  const { id } = await params;
  const conversation = await getConversationWithMessages(id);

  if (!conversation) {
    notFound();
  }

  return (
    <RoleGuard allowedRoles={['user', 'admin']}>
      <div className="h-screen flex flex-col bg-gray-50">
        <ChatInterface conversation={conversation} />
      </div>
    </RoleGuard>
  );
}
