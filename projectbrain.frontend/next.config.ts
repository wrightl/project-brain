import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
    output: 'standalone',
    images: {
        remotePatterns: [
            {
                protocol: 'https',
                hostname: '*',
            },
        ],
    },
    experimental: {
        proxyClientMaxBodySize: '100mb',
    },
};

export default nextConfig;
