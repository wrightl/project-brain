import { RoleGuard } from '@/components/auth/role-guard';
import { redirect } from 'next/navigation';

export default async function ChatIndexPage() {
  // Redirect to dashboard - users can start a chat from there
  // or we create a new conversation and redirect to it
  redirect('/dashboard');
}
