import { RoleGuard } from '@/_components/auth/role-guard';
import FileUploadForm from '@/_components/upload/file-upload-form';
import ResourceList from '@/_components/upload/resource-list';

export default async function UserUploadPage() {
    return (
        <RoleGuard allowedRoles={['user']}>
            <div className="space-y-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        Upload Files
                    </h1>
                    <p className="mt-1 text-sm text-gray-600">
                        Upload documents to personalize your AI assistant&apos;s
                        knowledge
                    </p>
                </div>

                <div className="bg-white shadow rounded-lg p-6">
                    <FileUploadForm />
                </div>

                {/* Info Box */}
                <div className="bg-indigo-50 border border-indigo-200 rounded-lg p-6">
                    <h3 className="text-sm font-medium text-indigo-900 mb-2">
                        About File Uploads
                    </h3>
                    <ul className="text-sm text-indigo-700 space-y-1 list-disc list-inside">
                        <li>
                            Your uploaded files are private and only accessible
                            to your AI assistant
                        </li>
                        <li>Supported formats: PDF, TXT, DOC, DOCX</li>
                        <li>
                            Files help the AI provide more personalized and
                            contextual responses
                        </li>
                        <li>
                            You can upload reference materials, notes, or any
                            documents you want the AI to know about
                        </li>
                    </ul>
                </div>

                {/* Resource List */}
                <ResourceList />
            </div>
        </RoleGuard>
    );
}
