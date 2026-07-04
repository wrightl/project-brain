import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
    output: 'standalone',
    images: {
        remotePatterns: [
            {
                protocol: 'https',
                hostname: '**.blob.core.windows.net',
            },
            {
                protocol: 'https',
                hostname: 's.gravatar.com',
            },
            {
                protocol: 'https',
                hostname: 'lh3.googleusercontent.com',
            },
            {
                protocol: 'https',
                hostname: 'assets.skool.com',
            },
        ],
    },
    experimental: {
        proxyClientMaxBodySize: '100mb',
    },
};

export default nextConfig;
