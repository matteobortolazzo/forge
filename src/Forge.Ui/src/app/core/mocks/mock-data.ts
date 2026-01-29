import { Task, TaskLog, Notification, BacklogItem, BacklogItemState, PipelineState, Priority } from '../../shared/models';

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

// Default mock repository ID
const MOCK_REPO_ID = 'repo-1';

// Mock Backlog Items
export const MOCK_BACKLOG_ITEMS: BacklogItem[] = [
  // New (2 items)
  {
    id: 'backlog-001',
    repositoryId: MOCK_REPO_ID,
    title: 'Add user authentication',
    description: 'Implement JWT-based authentication with login, logout, and token refresh functionality.',
    state: 'New',
    priority: 'high',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 0,
    completedTaskCount: 0,
    hasPendingGate: false,
    refiningIterations: 0,
    createdAt: daysAgo(5),
    updatedAt: daysAgo(5),
  },
  {
    id: 'backlog-002',
    repositoryId: MOCK_REPO_ID,
    title: 'Create dashboard charts',
    description: 'Add interactive charts showing task completion metrics and agent performance over time.',
    state: 'New',
    priority: 'medium',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 0,
    completedTaskCount: 0,
    hasPendingGate: false,
    refiningIterations: 0,
    createdAt: daysAgo(3),
    updatedAt: daysAgo(3),
  },

  // Executing (1 item with tasks)
  {
    id: 'backlog-006',
    repositoryId: MOCK_REPO_ID,
    title: 'Fix API rate limiting',
    description: 'Implement proper rate limiting on all API endpoints to prevent abuse.',
    state: 'Executing',
    priority: 'critical',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 3,
    completedTaskCount: 1,
    hasPendingGate: false,
    refiningIterations: 1,
    progress: { completed: 1, total: 3, percent: 33 },
    createdAt: daysAgo(2),
    updatedAt: minutesAgo(5),
  },

  // Done (1 item)
  {
    id: 'backlog-008',
    repositoryId: MOCK_REPO_ID,
    title: 'Setup CI/CD pipeline',
    description: 'Configure GitHub Actions for automated testing and deployment.',
    state: 'Done',
    priority: 'high',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 5,
    completedTaskCount: 5,
    hasPendingGate: false,
    refiningIterations: 1,
    progress: { completed: 5, total: 5, percent: 100 },
    createdAt: daysAgo(10),
    updatedAt: daysAgo(1),
  },
];

// Mock Tasks (for backlog-006)
export const MOCK_TASKS: Task[] = [
  {
    id: 'task-001',
    repositoryId: MOCK_REPO_ID,
    backlogItemId: 'backlog-006',
    title: 'Research existing rate limiting solutions',
    description: 'Analyze current API structure and research rate limiting approaches.',
    state: 'PrReady',
    priority: 'critical',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    executionOrder: 1,
    hasPendingGate: false,
    createdAt: hoursAgo(4),
    updatedAt: hoursAgo(2),
  },
  {
    id: 'task-002',
    repositoryId: MOCK_REPO_ID,
    backlogItemId: 'backlog-006',
    title: 'Implement rate limiting middleware',
    description: 'Add AspNetCoreRateLimit package and configure rate limiting.',
    state: 'Implementing',
    priority: 'critical',
    assignedAgentId: 'agent-001',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    executionOrder: 2,
    hasPendingGate: false,
    createdAt: hoursAgo(3),
    updatedAt: minutesAgo(5),
  },
  {
    id: 'task-003',
    repositoryId: MOCK_REPO_ID,
    backlogItemId: 'backlog-006',
    title: 'Add rate limit tests and documentation',
    description: 'Write integration tests and update API documentation.',
    state: 'Planning',
    priority: 'high',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    executionOrder: 3,
    hasPendingGate: false,
    createdAt: hoursAgo(2),
    updatedAt: hoursAgo(2),
  },
];

// Mock Logs for the active task (task-002 with agent)
export const MOCK_LOGS: TaskLog[] = [
  {
    id: 'log-001',
    taskId: 'task-002',
    type: 'info',
    content: 'Starting implementation of API rate limiting...',
    timestamp: minutesAgo(10),
  },
  {
    id: 'log-002',
    taskId: 'task-002',
    type: 'thinking',
    content: 'Analyzing current API structure to determine best approach for rate limiting. Need to consider: token bucket vs sliding window, per-user vs global limits, Redis for distributed tracking.',
    timestamp: minutesAgo(9),
  },
  {
    id: 'log-003',
    taskId: 'task-002',
    type: 'toolUse',
    content: 'Reading file: src/Forge.Api/Program.cs',
    toolName: 'Read',
    timestamp: minutesAgo(8),
  },
  {
    id: 'log-004',
    taskId: 'task-002',
    type: 'toolResult',
    content: 'File contents retrieved successfully (142 lines)',
    toolName: 'Read',
    timestamp: minutesAgo(8),
  },
  {
    id: 'log-005',
    taskId: 'task-002',
    type: 'thinking',
    content: 'The API uses minimal APIs pattern. I should add rate limiting middleware in the pipeline. Will use AspNetCoreRateLimit package for flexibility.',
    timestamp: minutesAgo(7),
  },
  {
    id: 'log-006',
    taskId: 'task-002',
    type: 'toolUse',
    content: 'Searching for existing rate limiting configuration...',
    toolName: 'Grep',
    timestamp: minutesAgo(6),
  },
  {
    id: 'log-007',
    taskId: 'task-002',
    type: 'toolResult',
    content: 'No existing rate limiting found in codebase',
    toolName: 'Grep',
    timestamp: minutesAgo(6),
  },
  {
    id: 'log-008',
    taskId: 'task-002',
    type: 'toolUse',
    content: 'Adding rate limiting package to project...',
    toolName: 'Bash',
    timestamp: minutesAgo(5),
  },
  {
    id: 'log-009',
    taskId: 'task-002',
    type: 'toolResult',
    content: 'dotnet add package AspNetCoreRateLimit --version 5.0.0\nPackage installed successfully',
    toolName: 'Bash',
    timestamp: minutesAgo(4),
  },
  {
    id: 'log-010',
    taskId: 'task-002',
    type: 'info',
    content: 'Creating rate limiting configuration...',
    timestamp: minutesAgo(3),
  },
  {
    id: 'log-011',
    taskId: 'task-002',
    type: 'toolUse',
    content: 'Writing file: src/Forge.Api/RateLimitConfig.cs',
    toolName: 'Write',
    timestamp: minutesAgo(2),
  },
  {
    id: 'log-012',
    taskId: 'task-002',
    type: 'toolResult',
    content: 'File created successfully',
    toolName: 'Write',
    timestamp: minutesAgo(2),
  },
];

// Mock Notifications
export const MOCK_NOTIFICATIONS: Notification[] = [
  {
    id: 'notif-001',
    title: 'Backlog Item Completed',
    message: 'Setup CI/CD pipeline has been completed successfully.',
    type: 'success',
    backlogItemId: 'backlog-008',
    read: true,
    createdAt: daysAgo(1),
  },
  {
    id: 'notif-002',
    title: 'Agent Started',
    message: 'Agent assigned to "Implement rate limiting middleware"',
    type: 'info',
    taskId: 'task-002',
    backlogItemId: 'backlog-006',
    read: false,
    createdAt: minutesAgo(10),
  },
];

// Helper to get all logs for a task
export function getLogsForTask(taskId: string): TaskLog[] {
  return MOCK_LOGS.filter(log => log.taskId === taskId);
}

// Helper to get task by ID
export function getTaskById(taskId: string): Task | undefined {
  return MOCK_TASKS.find(task => task.id === taskId);
}

// Helper to get tasks by state
export function getTasksByState(state: PipelineState): Task[] {
  return MOCK_TASKS.filter(task => task.state === state);
}

// Helper to get backlog item by ID
export function getBacklogItemById(id: string): BacklogItem | undefined {
  return MOCK_BACKLOG_ITEMS.find(item => item.id === id);
}

// Helper to get backlog items by state
export function getBacklogItemsByState(state: BacklogItemState): BacklogItem[] {
  return MOCK_BACKLOG_ITEMS.filter(item => item.state === state);
}
