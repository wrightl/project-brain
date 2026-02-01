import coreWebVitals from 'eslint-config-next/core-web-vitals';
import typescript from 'eslint-config-next/typescript';

const config = [
    ...coreWebVitals,
    ...typescript,
    // The Next.js/ESLint ecosystem evolves quickly; when upgrading deps we keep
    // some rules as warnings to avoid large refactors unrelated to the upgrade.
    {
        rules: {
            '@typescript-eslint/no-explicit-any': 'warn',
            'react/no-unescaped-entities': 'warn',
            'react-hooks/set-state-in-effect': 'warn',
            'react-hooks/error-boundaries': 'warn',
            'import/no-anonymous-default-export': 'off',
        },
    },
    // Allow CommonJS-style config files (Jest, etc.)
    {
        files: ['**/*.config.{js,cjs}', 'jest.config.js'],
        rules: {
            '@typescript-eslint/no-require-imports': 'off',
        },
    },
];

export default config;
