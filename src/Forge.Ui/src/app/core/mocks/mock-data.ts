import { Task, TaskLog, Notification, PipelineState, Priority } from '../../shared/models';

// Helper to generate IDs
const generateId = () => crypto.randomUUID();

// Helper to create dates
const daysAgo = (days: number) => {
  const date = new Date();
  date.setDate(date.getDate() - days);
  return date;
};

const hoursAgo = (hours: number) => {
  const date = new Date();
  date.setHours(date.getHours() - hours);
  return date;
};

const minutesAgo = (minutes: number) => {
  const date = new Date();
  date.setMinutes(date.getMinutes() - minutes);
  return date;
};

// Mock Tasks - 18 tasks distributed across all states
export const MOCK_TASKS: Task[] = [
  // Backlog (3 tasks)
  {
    id: 'task-001',
    title: 'Add user authentication',
    description: 'Implement JWT-based authentication with login, logout, and token refresh functionality.',
    state: 'Backlog',
    priority: 'high',
    hasError: false,
    createdAt: daysAgo(5),
    updatedAt: daysAgo(5),
  },
  {
    id: 'task-002',
    title: 'Create dashboard charts',
    description: 'Add interactive charts showing task completion metrics and agent performance over time.',
    state: 'Backlog',
    priority: 'medium',
    hasError: false,
    createdAt: daysAgo(3),
    updatedAt: daysAgo(3),
  },
  {
    id: 'task-003',
    title: 'Add dark mode support',
    description: 'Implement system-wide dark mode toggle with persistent user preference.',
    state: 'Backlog',
    priority: 'low',
    hasError: false,
    createdAt: daysAgo(2),
    updatedAt: daysAgo(2),
  },

  // Planning (2 tasks)
  {
    id: 'task-004',
    title: 'Implement drag-and-drop',
    description: 'Allow users to drag tasks between columns on the Kanban board.',
    state: 'Planning',
    priority: 'medium',
    hasError: false,
    createdAt: daysAgo(4),
    updatedAt: hoursAgo(12),
  },
  {
    id: 'task-005',
    title: 'Add task filtering',
    description: 'Implement filters for priority, assignee, and date range on the board view.',
    state: 'Planning',
    priority: 'low',
    hasError: false,
    createdAt: daysAgo(3),
    updatedAt: hoursAgo(8),
  },

  // Implementing (3 tasks - one with agent)
  {
    id: 'task-006',
    title: 'Fix API rate limiting',
    description: 'Implement proper rate limiting on all API endpoints to prevent abuse.',
    state: 'Implementing',
    priority: 'critical',
    assignedAgentId: 'agent-001',
    hasError: false,
    createdAt: daysAgo(2),
    updatedAt: minutesAgo(5),
  },
  {
    id: 'task-007',
    title: 'Add email notifications',
    description: 'Send email notifications when tasks are assigned or completed.',
    state: 'Implementing',
    priority: 'medium',
    hasError: false,
    createdAt: daysAgo(3),
    updatedAt: hoursAgo(2),
  },
  {
    id: 'task-008',
    title: 'Improve error handling',
    description: 'Add comprehensive error boundaries and user-friendly error messages.',
    state: 'Implementing',
    priority: 'high',
    hasError: true,
    errorMessage: 'Build failed: Cannot resolve module "@angular/forms"',
    createdAt: daysAgo(1),
    updatedAt: hoursAgo(1),
  },

  // Reviewing (3 tasks)
  {
    id: 'task-009',
    title: 'Refactor task service',
    description: 'Simplify the task service API and improve type safety.',
    state: 'Reviewing',
    priority: 'medium',
    hasError: false,
    createdAt: daysAgo(5),
    updatedAt: hoursAgo(4),
  },
  {
    id: 'task-010',
    title: 'Add loading states',
    description: 'Implement skeleton loaders and loading indicators throughout the app.',
    state: 'Reviewing',
    priority: 'low',
    hasError: false,
    createdAt: daysAgo(4),
    updatedAt: hoursAgo(6),
  },
  {
    id: 'task-011',
    title: 'Update API documentation',
    description: 'Add OpenAPI spec and update README with new endpoints.',
    state: 'Reviewing',
    priority: 'low',
    hasError: false,
    createdAt: daysAgo(2),
    updatedAt: hoursAgo(3),
  },

  // Testing (3 tasks)
  {
    id: 'task-012',
    title: 'Add unit tests for stores',
    description: 'Write comprehensive unit tests for all signal stores.',
    state: 'Testing',
    priority: 'high',
    hasError: false,
    createdAt: daysAgo(6),
    updatedAt: hoursAgo(5),
  },
  {
    id: 'task-013',
    title: 'Fix flaky E2E tests',
    description: 'Investigate and fix intermittent failures in Playwright tests.',
    state: 'Testing',
    priority: 'critical',
    hasError: true,
    errorMessage: 'Test timeout: element not found after 30s',
    createdAt: daysAgo(3),
    updatedAt: hoursAgo(2),
  },
  {
    id: 'task-014',
    title: 'Performance testing',
    description: 'Run load tests and optimize slow database queries.',
    state: 'Testing',
    priority: 'medium',
    hasError: false,
    createdAt: daysAgo(4),
    updatedAt: hoursAgo(7),
  },

  // PR Ready (2 tasks)
  {
    id: 'task-015',
    title: 'Add keyboard shortcuts',
    description: 'Implement keyboard shortcuts for common actions (create task, navigate).',
    state: 'PrReady',
    priority: 'low',
    hasError: false,
    createdAt: daysAgo(7),
    updatedAt: hoursAgo(10),
  },
  {
    id: 'task-016',
    title: 'Improve accessibility',
    description: 'Add ARIA labels and improve screen reader support.',
    state: 'PrReady',
    priority: 'high',
    hasError: false,
    createdAt: daysAgo(5),
    updatedAt: hoursAgo(8),
  },

  // Done (2 tasks)
  {
    id: 'task-017',
    title: 'Setup CI/CD pipeline',
    description: 'Configure GitHub Actions for automated testing and deployment.',
    state: 'Done',
    priority: 'high',
    hasError: false,
    createdAt: daysAgo(10),
    updatedAt: daysAgo(1),
  },
  {
    id: 'task-018',
    title: 'Initial project setup',
    description: 'Create Angular project with Tailwind CSS and configure ESLint.',
    state: 'Done',
    priority: 'critical',
    hasError: false,
    createdAt: daysAgo(14),
    updatedAt: daysAgo(12),
  },
];

// Mock Logs for the active task (task-006 with agent)
export const MOCK_LOGS: TaskLog[] = [
  {
    id: 'log-001',
    taskId: 'task-006',
    type: 'info',
    content: 'Starting implementation of API rate limiting...',
    timestamp: minutesAgo(10),
  },
  {
    id: 'log-002',
    taskId: 'task-006',
    type: 'thinking',
    content: 'Analyzing current API structure to determine best approach for rate limiting. Need to consider: token bucket vs sliding window, per-user vs global limits, Redis for distributed tracking.',
    timestamp: minutesAgo(9),
  },
  {
    id: 'log-003',
    taskId: 'task-006',
    type: 'toolUse',
    content: 'Reading file: src/Forge.Api/Program.cs',
    toolName: 'Read',
    timestamp: minutesAgo(8),
  },
  {
    id: 'log-004',
    taskId: 'task-006',
    type: 'toolResult',
    content: 'File contents retrieved successfully (142 lines)',
    toolName: 'Read',
    timestamp: minutesAgo(8),
  },
  {
    id: 'log-005',
    taskId: 'task-006',
    type: 'thinking',
    content: 'The API uses minimal APIs pattern. I should add rate limiting middleware in the pipeline. Will use AspNetCoreRateLimit package for flexibility.',
    timestamp: minutesAgo(7),
  },
  {
    id: 'log-006',
    taskId: 'task-006',
    type: 'toolUse',
    content: 'Searching for existing rate limiting configuration...',
    toolName: 'Grep',
    timestamp: minutesAgo(6),
  },
  {
    id: 'log-007',
    taskId: 'task-006',
    type: 'toolResult',
    content: 'No existing rate limiting found in codebase',
    toolName: 'Grep',
    timestamp: minutesAgo(6),
  },
  {
    id: 'log-008',
    taskId: 'task-006',
    type: 'toolUse',
    content: 'Adding rate limiting package to project...',
    toolName: 'Bash',
    timestamp: minutesAgo(5),
  },
  {
    id: 'log-009',
    taskId: 'task-006',
    type: 'toolResult',
    content: 'dotnet add package AspNetCoreRateLimit --version 5.0.0\nPackage installed successfully',
    toolName: 'Bash',
    timestamp: minutesAgo(4),
  },
  {
    id: 'log-010',
    taskId: 'task-006',
    type: 'info',
    content: 'Creating rate limiting configuration...',
    timestamp: minutesAgo(3),
  },
  {
    id: 'log-011',
    taskId: 'task-006',
    type: 'toolUse',
    content: 'Writing file: src/Forge.Api/RateLimitConfig.cs',
    toolName: 'Write',
    timestamp: minutesAgo(2),
  },
  {
    id: 'log-012',
    taskId: 'task-006',
    type: 'toolResult',
    content: 'File created successfully',
    toolName: 'Write',
    timestamp: minutesAgo(2),
  },
];

// Additional logs for error task (task-008)
export const MOCK_ERROR_LOGS: TaskLog[] = [
  {
    id: 'log-e01',
    taskId: 'task-008',
    type: 'info',
    content: 'Starting error handling improvements...',
    timestamp: hoursAgo(2),
  },
  {
    id: 'log-e02',
    taskId: 'task-008',
    type: 'toolUse',
    content: 'Building project to verify changes...',
    toolName: 'Bash',
    timestamp: hoursAgo(1),
  },
  {
    id: 'log-e03',
    taskId: 'task-008',
    type: 'error',
    content: 'Build failed: Cannot resolve module "@angular/forms". Did you mean to install it as a dependency?',
    toolName: 'Bash',
    timestamp: hoursAgo(1),
  },
];

// Mock Notifications
export const MOCK_NOTIFICATIONS: Notification[] = [
  {
    id: 'notif-001',
    title: 'Task Completed',
    message: 'Setup CI/CD pipeline has been completed successfully.',
    type: 'success',
    taskId: 'task-017',
    read: true,
    createdAt: daysAgo(1),
  },
  {
    id: 'notif-002',
    title: 'Agent Error',
    message: 'Build failed while implementing error handling improvements.',
    type: 'error',
    taskId: 'task-008',
    read: false,
    createdAt: hoursAgo(1),
  },
  {
    id: 'notif-003',
    title: 'Agent Started',
    message: 'Agent assigned to "Fix API rate limiting"',
    type: 'info',
    taskId: 'task-006',
    read: false,
    createdAt: minutesAgo(10),
  },
  {
    id: 'notif-004',
    title: 'Test Failed',
    message: 'E2E tests are failing intermittently.',
    type: 'warning',
    taskId: 'task-013',
    read: false,
    createdAt: hoursAgo(2),
  },
  {
    id: 'notif-005',
    title: 'PR Ready',
    message: 'Keyboard shortcuts implementation is ready for review.',
    type: 'success',
    taskId: 'task-015',
    read: true,
    createdAt: hoursAgo(10),
  },
];

// Helper to get all logs for a task
export function getLogsForTask(taskId: string): TaskLog[] {
  const allLogs = [...MOCK_LOGS, ...MOCK_ERROR_LOGS];
  return allLogs.filter(log => log.taskId === taskId);
}

// Helper to get task by ID
export function getTaskById(taskId: string): Task | undefined {
  return MOCK_TASKS.find(task => task.id === taskId);
}

// Helper to get tasks by state
export function getTasksByState(state: PipelineState): Task[] {
  return MOCK_TASKS.filter(task => task.state === state);
}
