import { createApiRoute } from '@/_lib/api-route-handler';
import { getAccessToken } from '@/_lib/auth';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest, NextResponse } from 'next/server';

export const POST = createApiRoute(async (req: NextRequest) => {
    const fileData = await req.formData();

    console.log('Received file upload request');
    console.log('Request body:', fileData);

    // Loop through all received files and append them.
    // Use the same key 'file' to send an array of files.
    const formData = new FormData();
    const files = fileData.getAll('file');
    for (const file of files) {
        formData.append('file', file);
    }

    const API_URL = process.env.API_SERVER_URL || 'https://localhost:7585';
    const accessToken = await getAccessToken();

    const data = await fetch(`${API_URL}/resource/upload`, {
        method: 'POST',
        body: formData,
        // **IMPORTANT**: Do NOT set the 'Content-Type' header here.
        // fetch will automatically set it to 'multipart/form-data'
        // with the correct 'boundary' string.

        // Add any necessary auth headers for your backend
        headers: {
            Authorization: `Bearer ${accessToken}`,
        },
    });

    // const data = await callBackendApi('/resource/upload', {
    //     method: 'POST',
    //     body: formData,
    //     isFormData: true,
    // });

    return NextResponse.json(await data.json(), { status: 201 });
});
