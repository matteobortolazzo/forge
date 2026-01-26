// Pipeline States - ordered from start to finish
export const PIPELINE_STATES = [
  'Backlog',
  'Planning',
  'Implementing',
  'Reviewing',
  'Testing',
  'PrReady',
  'Done',
] as const;

export type PipelineState = (typeof PIPELINE_STATES)[number];

// Priority Levels
export const PRIORITIES = ['low', 'medium', 'high', 'critical'] as const;
export type Priority = (typeof PRIORITIES)[number];

// Log Types for agent output
export const LOG_TYPES = ['info', 'toolUse', 'toolResult', 'error', 'thinking'] as const;
export type LogType = (typeof LOG_TYPES)[number];

// Task Progress for parent tasks
export interface TaskProgress {
  completed: number;
  total: number;
  percent: number;
}

// Task Interface
export interface Task {
  id: string;
  title: string;
  description: string;
  state: PipelineState;
  priority: Priority;
  assignedAgentId?: string;
  hasError: boolean;
  errorMessage?: string;
  isPaused: boolean;
  pauseReason?: string;
  pausedAt?: Date;
  retryCount: number;
  maxRetries: number;
  createdAt: Date;
  updatedAt: Date;
  // Hierarchy fields
  parentId?: string;
  childCount: number;
  derivedState?: PipelineState;
  children?: Task[];
  progress?: TaskProgress;
}

// Task Log Interface
export interface TaskLog {
  id: string;
  taskId: string;
  type: LogType;
  content: string;
  timestamp: Date;
  toolName?: string;
}

// Notification Interface
export interface Notification {
  id: string;
  title: string;
  message: string;
  type: 'info' | 'success' | 'warning' | 'error';
  taskId?: string;
  read: boolean;
  createdAt: Date;
}

// DTOs for API operations
export interface CreateTaskDto {
  title: string;
  description: string;
  priority: Priority;
}

export interface UpdateTaskDto {
  title?: string;
  description?: string;
  priority?: Priority;
}

export interface TransitionTaskDto {
  targetState: PipelineState;
}

// Hierarchy DTOs
export interface CreateSubtaskDto {
  title: string;
  description: string;
  priority: Priority;
}

export interface SplitTaskDto {
  subtasks: CreateSubtaskDto[];
}

export interface SplitTaskResultDto {
  parent: Task;
  children: Task[];
}

// Event types for SSE
export type ServerEventType =
  | 'task:created'
  | 'task:updated'
  | 'task:deleted'
  | 'task:log'
  | 'task:paused'
  | 'task:resumed'
  | 'task:split'
  | 'task:childAdded'
  | 'agent:statusChanged'
  | 'scheduler:taskScheduled'
  | 'notification:new';

// SSE Event Payloads
export interface TaskSplitPayload {
  parent: Task;
  children: Task[];
}

export interface ChildAddedPayload {
  parentId: string;
  child: Task;
}

export interface ServerEvent {
  type: ServerEventType;
  payload: unknown;
  timestamp: Date;
}

// Agent Status
export interface AgentStatus {
  isRunning: boolean;
  currentTaskId?: string;
  startedAt?: Date;
}

// Scheduler Status
export interface SchedulerStatus {
  isEnabled: boolean;
  isAgentRunning: boolean;
  currentTaskId?: string;
  pendingTaskCount: number;
  pausedTaskCount: number;
}

// Pause Task DTO
export interface PauseTaskDto {
  reason: string;
}

// Repository Info
export interface RepositoryInfo {
  name: string;
  path: string;
  branch?: string;
  commitHash?: string;
  remoteUrl?: string;
  isDirty: boolean;
  isGitRepository: boolean;
}
