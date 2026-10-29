/**
 * ============================================================
 * FILE: eslint.config.js
 * TÁC DỤNG:
 *   Luật kiểm tra code (lint) cho JS và Vue → cả team viết cùng một kiểu,
 *   bắt lỗi sớm (biến không dùng, sai tên biến, sai cú pháp template...).
 * CÁCH DÙNG:
 *   - `npm run lint`  : kiểm tra và tự sửa lỗi có thể sửa.
 *   - Cài extension ESLint trong VS Code để thấy lỗi ngay khi gõ.
 * ============================================================
 */
import js from '@eslint/js';
import pluginVue from 'eslint-plugin-vue';
import globals from 'globals';

export default [
  { ignores: ['dist/**', 'node_modules/**'] },
  js.configs.recommended,
  ...pluginVue.configs['flat/recommended'],
  {
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: { ...globals.browser, ...globals.node },
    },
    rules: {
      'vue/multi-word-component-names': 'off', // cho phép tên 1 từ: Login.vue
      'vue/max-attributes-per-line': 'off',
      'vue/singleline-html-element-content-newline': 'off',
      'vue/html-self-closing': 'off',
      'no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
    },
  },
];
