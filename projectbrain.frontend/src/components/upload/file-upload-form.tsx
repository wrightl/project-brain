'use client';

import { useState } from 'react';
import { uploadKnowledgeFiles } from '@/lib/api-client';
import { CloudArrowUpIcon, XMarkIcon, CheckCircleIcon } from '@heroicons/react/24/outline';

interface UploadedFile {
  file: File;
  status: 'pending' | 'uploading' | 'success' | 'error';
  message?: string;
  chunks?: number;
}

export default function FileUploadForm() {
  const [files, setFiles] = useState<UploadedFile[]>([]);
  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = () => {
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const droppedFiles = Array.from(e.dataTransfer.files);
    addFiles(droppedFiles);
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(e.target.files || []);
    addFiles(selectedFiles);
  };

  const addFiles = (newFiles: File[]) => {
    const uploadedFiles: UploadedFile[] = newFiles.map((file) => ({
      file,
      status: 'pending',
    }));
    setFiles((prev) => [...prev, ...uploadedFiles]);
  };

  const removeFile = (index: number) => {
    setFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const handleUpload = async () => {
    if (files.length === 0) return;

    setIsUploading(true);

    try {
      // Update all files to uploading status
      setFiles((prev) =>
        prev.map((f) => ({ ...f, status: 'uploading' as const }))
      );

      const filesToUpload = files.map((f) => f.file);
      const results = await uploadKnowledgeFiles(filesToUpload);

      // Update files with results
      setFiles((prev) =>
        prev.map((f, i) => {
          const result = results[i];
          return {
            ...f,
            status: result.status === 'uploaded' ? 'success' : 'error',
            message: result.message,
            chunks: result.chunks,
          };
        })
      );
    } catch (error) {
      console.error('Upload failed:', error);
      setFiles((prev) =>
        prev.map((f) => ({
          ...f,
          status: 'error',
          message: error instanceof Error ? error.message : 'Upload failed',
        }))
      );
    } finally {
      setIsUploading(false);
    }
  };

  const clearCompleted = () => {
    setFiles((prev) => prev.filter((f) => f.status !== 'success'));
  };

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  };

  return (
    <div className="space-y-6">
      {/* Drop Zone */}
      <div
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        className={`border-2 border-dashed rounded-lg p-8 text-center transition-colors ${
          isDragging
            ? 'border-indigo-500 bg-indigo-50'
            : 'border-gray-300 hover:border-gray-400'
        }`}
      >
        <CloudArrowUpIcon className="mx-auto h-12 w-12 text-gray-400" />
        <div className="mt-4">
          <label
            htmlFor="file-upload"
            className="cursor-pointer text-indigo-600 hover:text-indigo-500 font-medium"
          >
            Choose files
          </label>
          <input
            id="file-upload"
            type="file"
            multiple
            onChange={handleFileSelect}
            className="sr-only"
          />
          <span className="text-gray-600"> or drag and drop</span>
        </div>
        <p className="mt-2 text-xs text-gray-500">
          PDF, DOC, DOCX, TXT up to 10MB each
        </p>
      </div>

      {/* File List */}
      {files.length > 0 && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-medium text-gray-900">
              Files ({files.length})
            </h3>
            <button
              onClick={clearCompleted}
              className="text-sm text-gray-600 hover:text-gray-900"
            >
              Clear completed
            </button>
          </div>

          <ul className="divide-y divide-gray-200 border border-gray-200 rounded-lg">
            {files.map((uploadedFile, index) => (
              <li key={index} className="p-4">
                <div className="flex items-center justify-between">
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-900 truncate">
                      {uploadedFile.file.name}
                    </p>
                    <p className="text-xs text-gray-500">
                      {formatFileSize(uploadedFile.file.size)}
                      {uploadedFile.chunks && ` • ${uploadedFile.chunks} chunks`}
                    </p>
                    {uploadedFile.message && (
                      <p
                        className={`text-xs mt-1 ${
                          uploadedFile.status === 'error'
                            ? 'text-red-600'
                            : 'text-gray-500'
                        }`}
                      >
                        {uploadedFile.message}
                      </p>
                    )}
                  </div>
                  <div className="ml-4 flex items-center space-x-2">
                    {uploadedFile.status === 'success' && (
                      <CheckCircleIcon className="h-5 w-5 text-green-500" />
                    )}
                    {uploadedFile.status === 'error' && (
                      <XMarkIcon className="h-5 w-5 text-red-500" />
                    )}
                    {uploadedFile.status === 'uploading' && (
                      <div className="animate-spin h-5 w-5 border-2 border-indigo-500 border-t-transparent rounded-full" />
                    )}
                    {uploadedFile.status === 'pending' && (
                      <button
                        onClick={() => removeFile(index)}
                        className="text-gray-400 hover:text-gray-600"
                      >
                        <XMarkIcon className="h-5 w-5" />
                      </button>
                    )}
                  </div>
                </div>
              </li>
            ))}
          </ul>

          {/* Upload Button */}
          <div className="flex justify-end">
            <button
              onClick={handleUpload}
              disabled={isUploading || files.every((f) => f.status !== 'pending')}
              className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
            >
              {isUploading ? 'Uploading...' : 'Upload Files'}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
