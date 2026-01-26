import { test, expect } from '@playwright/test';

const API_BASE = 'http://localhost:5140';

test.describe('Task Workflow', () => {
  test.beforeEach(async ({ request }) => {
    // Reset mock to default state before each test
    await request.post(`${API_BASE}/api/mock/reset`);
  });

  test('should display Kanban board with all columns', async ({ page }) => {
    await page.goto('/');

    // Wait for board to load
    await expect(page.getByRole('heading', { name: 'Forge' })).toBeVisible();

    // Verify all pipeline columns are visible
    const columns = ['Backlog', 'Planning', 'Implementing', 'Reviewing', 'Testing', 'PR Ready', 'Done'];
    for (const column of columns) {
      await expect(page.getByText(column, { exact: true })).toBeVisible();
    }
  });

  test('should create a new task', async ({ page }) => {
    await page.goto('/');

    // Click "New Task" button
    await page.getByRole('button', { name: 'New Task' }).click();

    // Fill in task form
    await page.getByLabel('Title').fill('Test Task from E2E');
    await page.getByLabel('Description').fill('This task was created by Playwright E2E test');

    // Select priority
    await page.getByLabel('Priority').selectOption('High');

    // Submit the form
    await page.getByRole('button', { name: 'Create' }).click();

    // Verify task appears in Backlog column
    await expect(page.getByText('Test Task from E2E')).toBeVisible();
  });

  test('should navigate to task detail view', async ({ page, request }) => {
    // Create a task first via API
    const createResponse = await request.post(`${API_BASE}/api/tasks`, {
      data: {
        title: 'Detail View Test Task',
        description: 'Testing task detail navigation',
        priority: 'Medium',
      },
    });
    expect(createResponse.ok()).toBeTruthy();
    const task = await createResponse.json();

    await page.goto('/');

    // Click on the task card
    await page.getByText('Detail View Test Task').click();

    // Should navigate to task detail page
    await expect(page).toHaveURL(`/tasks/${task.id}`);

    // Verify task details are visible
    await expect(page.getByText('Detail View Test Task')).toBeVisible();
    await expect(page.getByText('Testing task detail navigation')).toBeVisible();
  });

  test('should start agent on task and display logs', async ({ page, request }) => {
    // Create a task in Planning state
    const createResponse = await request.post(`${API_BASE}/api/tasks`, {
      data: {
        title: 'Agent Test Task',
        description: 'Testing agent execution with mock',
        priority: 'High',
      },
    });
    expect(createResponse.ok()).toBeTruthy();
    const task = await createResponse.json();

    // Transition to Planning state
    await request.post(`${API_BASE}/api/tasks/${task.id}/transition`, {
      data: { targetState: 'Planning' },
    });

    // Navigate to task detail
    await page.goto(`/tasks/${task.id}`);

    // Start agent
    await page.getByRole('button', { name: /start agent/i }).click();

    // Wait for agent output to appear (mock messages)
    await expect(page.getByText(/analyze the task/i)).toBeVisible({ timeout: 10000 });

    // Verify some agent output is visible
    await expect(page.getByText(/implementation is complete/i)).toBeVisible({ timeout: 15000 });
  });

  test('should handle task state transitions', async ({ page, request }) => {
    // Create a task
    const createResponse = await request.post(`${API_BASE}/api/tasks`, {
      data: {
        title: 'Transition Test Task',
        description: 'Testing state transitions',
        priority: 'Medium',
      },
    });
    expect(createResponse.ok()).toBeTruthy();
    const task = await createResponse.json();

    await page.goto('/');

    // Task should be in Backlog
    const backlogColumn = page.locator('app-task-column', { has: page.getByText('Backlog', { exact: true }) });
    await expect(backlogColumn.getByText('Transition Test Task')).toBeVisible();

    // Navigate to task detail
    await page.getByText('Transition Test Task').click();

    // Transition to Planning
    await page.getByRole('button', { name: /move to planning/i }).click();

    // Go back to board
    await page.goto('/');

    // Task should now be in Planning column
    const planningColumn = page.locator('app-task-column', { has: page.getByText('Planning', { exact: true }) });
    await expect(planningColumn.getByText('Transition Test Task')).toBeVisible();
  });

  test('mock status endpoint should confirm mock mode is enabled', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/mock/status`);
    expect(response.ok()).toBeTruthy();

    const status = await response.json();
    expect(status.mockModeEnabled).toBe(true);
    expect(status.defaultScenarioId).toBe('default');
    expect(status.availableScenarios).toContain('default');
    expect(status.availableScenarios).toContain('quick-success');
    expect(status.availableScenarios).toContain('error');
  });

  test('should be able to switch mock scenarios', async ({ request }) => {
    // Set quick-success scenario as default
    const setResponse = await request.post(`${API_BASE}/api/mock/scenario`, {
      data: { scenarioId: 'quick-success' },
    });
    expect(setResponse.ok()).toBeTruthy();

    // Verify scenario was set
    const statusResponse = await request.get(`${API_BASE}/api/mock/status`);
    const status = await statusResponse.json();
    expect(status.defaultScenarioId).toBe('quick-success');

    // Reset to default
    await request.post(`${API_BASE}/api/mock/reset`);

    // Verify reset
    const resetStatusResponse = await request.get(`${API_BASE}/api/mock/status`);
    const resetStatus = await resetStatusResponse.json();
    expect(resetStatus.defaultScenarioId).toBe('default');
  });
});

test.describe('Scheduler', () => {
  test.beforeEach(async ({ request }) => {
    await request.post(`${API_BASE}/api/mock/reset`);
  });

  test('should display scheduler status', async ({ page }) => {
    await page.goto('/');

    // Scheduler status component should be visible
    await expect(page.locator('app-scheduler-status')).toBeVisible();
  });

  test('should toggle scheduler enabled/disabled', async ({ page, request }) => {
    await page.goto('/');

    // Check initial status via API
    const initialStatus = await request.get(`${API_BASE}/api/scheduler/status`);
    const status = await initialStatus.json();

    if (status.enabled) {
      // Disable scheduler
      await request.post(`${API_BASE}/api/scheduler/disable`);
    }

    // Enable scheduler
    await request.post(`${API_BASE}/api/scheduler/enable`);
    const enabledStatus = await request.get(`${API_BASE}/api/scheduler/status`);
    const enabled = await enabledStatus.json();
    expect(enabled.enabled).toBe(true);

    // Disable scheduler
    await request.post(`${API_BASE}/api/scheduler/disable`);
    const disabledStatus = await request.get(`${API_BASE}/api/scheduler/status`);
    const disabled = await disabledStatus.json();
    expect(disabled.enabled).toBe(false);
  });
});

test.describe('Notifications', () => {
  test('should display notification panel', async ({ page }) => {
    await page.goto('/');

    // Notification panel should have a bell icon button
    const notificationButton = page.locator('app-notification-panel button');
    await expect(notificationButton).toBeVisible();
  });
});

test.describe('Error Scenarios', () => {
  test('agent failure should be handled gracefully', async ({ page, request }) => {
    // Set error scenario
    await request.post(`${API_BASE}/api/mock/scenario`, {
      data: { scenarioId: 'error' },
    });

    // Create a task in Planning state
    const createResponse = await request.post(`${API_BASE}/api/tasks`, {
      data: {
        title: 'Error Test Task',
        description: 'Testing error scenario',
        priority: 'High',
      },
    });
    const task = await createResponse.json();

    await request.post(`${API_BASE}/api/tasks/${task.id}/transition`, {
      data: { targetState: 'Planning' },
    });

    // Navigate to task detail
    await page.goto(`/tasks/${task.id}`);

    // Start agent
    await page.getByRole('button', { name: /start agent/i }).click();

    // Should see error message in output
    await expect(page.getByText(/encountered an error/i)).toBeVisible({ timeout: 10000 });

    // Reset mock for other tests
    await request.post(`${API_BASE}/api/mock/reset`);
  });
});
