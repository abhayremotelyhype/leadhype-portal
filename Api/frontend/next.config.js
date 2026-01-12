/** @type {import('next').NextConfig} */
const nextConfig = {
  typescript: {
    ignoreBuildErrors: false,
  },
  eslint: {
    ignoreDuringBuilds: false,
  },
  // Keep trailing slashes for consistent routing
  // trailingSlash: false,
  // Add webpack configuration to prevent chunk loading issues
  webpack: (config, { dev, isServer }) => {
    if (dev && !isServer) {
      // Ensure stable chunk naming in development
      config.optimization = {
        ...config.optimization,
        moduleIds: 'named',
        chunkIds: 'named',
      };
    }
    return config;
  },
  async rewrites() {
    return [
      {
        source: '/api/docs',
        destination: 'http://localhost:5010/api/docs/', // Explicitly add trailing slash for Scalar
      },
      {
        source: '/api/scalar.js',
        destination: 'http://localhost:5010/api/docs/scalar.js', // Fix Scalar JS path
      },
      {
        source: '/api/scalar.aspnetcore.js',
        destination: 'http://localhost:5010/api/docs/scalar.aspnetcore.js', // Fix Scalar ASP.NET JS path
      },
      {
        source: '/api/docs/:path*',
        destination: 'http://localhost:5010/api/docs/:path*', // Proxy Scalar assets
      },
      {
        source: '/api/:path*',
        destination: 'http://localhost:5010/api/:path*', // Proxy to ASP.NET Core API
      },
    ];
  },
};

module.exports = nextConfig;