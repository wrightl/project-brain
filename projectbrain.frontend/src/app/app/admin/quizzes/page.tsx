import { RoleGuard } from '@/_components/auth/role-guard';
import QuizManagementComponent from './_components/quiz-management-component';

import { AppRoles } from '@/_lib/roles';
export default async function AdminQuizzesPage() {
    return (
        <RoleGuard allowedRoles={[AppRoles.Admin]}>
            <QuizManagementComponent />
        </RoleGuard>
    );
}

