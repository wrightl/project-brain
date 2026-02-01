const nextJest = require('next/jest')

const createJestConfig = nextJest({
  // Provide the path to your Next.js app to load next.config.js and .env files in your test environment
  dir: './',
})

// Add any custom config to be passed to Jest
const customJestConfig = {
  // Polyfills that must run before any other imports (e.g. MSW/undici globals).
  setupFiles: ['<rootDir>/jest.polyfills.js'],
  setupFilesAfterEnv: ['<rootDir>/jest.setup.js'],
  testEnvironment: 'jest-environment-jsdom',
  // Watchman can fail in restricted/sandboxed environments (e.g. CI containers).
  watchman: false,
  // Prevent haste-map collisions with Next.js standalone output.
  modulePathIgnorePatterns: ['<rootDir>/.next/'],
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
  },
  collectCoverageFrom: [
    'src/**/*.{js,jsx,ts,tsx}',
    '!src/**/*.d.ts',
    '!src/**/*.stories.{js,jsx,ts,tsx}',
    '!src/**/__tests__/**',
  ],
  testMatch: [
    '<rootDir>/src/**/__tests__/**/*.{js,jsx,ts,tsx}',
    '<rootDir>/src/**/*.{spec,test}.{js,jsx,ts,tsx}',
  ],
}

// Export an async config so we can *override* next/jest defaults that would
// otherwise ignore transforming MSW's ESM dependencies in node_modules.
module.exports = async () => {
  const config = await createJestConfig(customJestConfig)()

  // MSW v2 ships ESM dependencies that Jest must transform.
  // next/jest includes default transformIgnorePatterns, so we overwrite them.
  config.transformIgnorePatterns = [
    '/node_modules/(?!.pnpm)(?!(geist|msw|@mswjs|until-async)/)',
    '/node_modules/.pnpm/(?!(geist|msw|@mswjs|until-async)@)',
    '^.+\\.module\\.(css|sass|scss)$',
  ]

  return config
}
