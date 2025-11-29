import { createApiRoute } from '@/_lib/api-route-handler';
import { getAccessToken } from '@/_lib/auth';
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

    const data = await fetch(`${API_URL}/resource/upload/shared`, {
        method: 'POST',
        body: formData,
        headers: {
            Authorization: `Bearer ${accessToken}`,
        },
    });

    return NextResponse.json(await data.json(), { status: 201 });
});
