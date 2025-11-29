import { RoleGuard } from '@/_components/auth/role-guard';
import QuizManagementComponent from './_components/quiz-management-component';

export default async function AdminQuizzesPage() {
    return (
        <RoleGuard allowedRoles={['admin']}>
            <QuizManagementComponent />
        </RoleGuard>
    );
}

