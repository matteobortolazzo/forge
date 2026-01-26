import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for E2E tests.
 * Runs both backend (in mock mode) and frontend during tests.
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: process.env['CI'] ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
  ],

  webServer: [
    {
      // Start backend in mock mode
      command: 'dotnet run --launch-profile e2e',
      cwd: '../Forge.Api/Forge.Api',
      url: 'http://localhost:5140/api/mock/status',
      reuseExistingServer: !process.env['CI'],
      timeout: 120 * 1000,
      stdout: 'pipe',
      stderr: 'pipe',
    },
    {
      // Start frontend
      command: 'npm run start',
      cwd: '..',
      url: 'http://localhost:4200',
      reuseExistingServer: !process.env['CI'],
      timeout: 120 * 1000,
      stdout: 'pipe',
      stderr: 'pipe',
    },
  ],
});
