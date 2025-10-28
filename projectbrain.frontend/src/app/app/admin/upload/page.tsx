import { RoleGuard } from '@/_components/auth/role-guard';
import FileUploadForm from '@/_components/upload/file-upload-form';

export default async function AdminUploadPage() {
    return (
        <RoleGuard allowedRoles={['admin']}>
            <div className="space-y-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        Upload Knowledge Base Files
                    </h1>
                    <p className="mt-1 text-sm text-gray-600">
                        Upload documents to enhance the AI assistant&apos;s
                        knowledge
                    </p>
                </div>

                <div className="bg-white shadow rounded-lg p-6">
                    <FileUploadForm />
                </div>

                {/* Upload Guidelines */}
                <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
                    <h3 className="text-sm font-medium text-blue-900 mb-2">
                        Upload Guidelines
                    </h3>
                    <ul className="text-sm text-blue-700 space-y-1 list-disc list-inside">
                        <li>Supported file types: PDF, TXT, DOC, DOCX</li>
                        <li>Maximum file size: 10MB per file</li>
                        <li>
                            Files will be processed and indexed for AI search
                        </li>
                        <li>
                            Processing may take a few moments depending on file
                            size
                        </li>
                    </ul>
                </div>
            </div>
        </RoleGuard>
    );
}
