// Backlog Item States - ordered from start to finish
export const BACKLOG_ITEM_STATES = [
  'New',
  'Refining',
  'Ready',
  'Splitting',
  'Executing',
  'Done',
] as const;

export type BacklogItemState = (typeof BACKLOG_ITEM_STATES)[number];

// Pipeline States - ordered from start to finish (for tasks)
// Flow: Planning → Implementing → PrReady
export const PIPELINE_STATES = [
  'Planning',
  'Implementing',
  'PrReady',
] as const;

export type PipelineState = (typeof PIPELINE_STATES)[number];

// Priority Levels
export const PRIORITIES = ['low', 'medium', 'high', 'critical'] as const;
export type Priority = (typeof PRIORITIES)[number];

// Log Types for agent output
export const LOG_TYPES = ['info', 'toolUse', 'toolResult', 'error', 'thinking'] as const;
export type LogType = (typeof LOG_TYPES)[number];

// Artifact Types produced by agents
export const ARTIFACT_TYPES = [
  'task_split',
  'plan',
  'implementation',
  'test',
  'general',
] as const;
export type ArtifactType = (typeof ARTIFACT_TYPES)[number];

// Backlog Item Progress
export interface BacklogItemProgress {
  completed: number;
  total: number;
  percent: number;
}

// Backlog Item Interface
export interface BacklogItem {
  id: string;
  repositoryId: string;
  title: string;
  description: string;
  state: BacklogItemState;
  priority: Priority;
  acceptanceCriteria?: string;
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
  taskCount: number;
  completedTaskCount: number;
  confidenceScore?: number;
  hasPendingGate: boolean;
  refiningIterations: number;
  progress?: BacklogItemProgress;
}

// Task Interface (belongs to a BacklogItem)
export interface Task {
  id: string;
  repositoryId: string;
  backlogItemId: string;
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
  executionOrder: number;
  confidenceScore?: number;
  hasPendingGate: boolean;
}

// Agent Artifact Interface
export interface Artifact {
  id: string;
  taskId?: string;
  backlogItemId?: string;
  producedInState: PipelineState | BacklogItemState;
  artifactType: ArtifactType;
  content: string;
  createdAt: Date;
  agentId?: string;
}

// Task Log Interface
export interface TaskLog {
  id: string;
  taskId?: string;
  backlogItemId?: string;
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
  backlogItemId?: string;
  read: boolean;
  createdAt: Date;
}

// Backlog Item DTOs
export interface CreateBacklogItemDto {
  title: string;
  description: string;
  priority?: Priority;
  acceptanceCriteria?: string;
}

export interface UpdateBacklogItemDto {
  title?: string;
  description?: string;
  priority?: Priority;
  acceptanceCriteria?: string;
}

export interface TransitionBacklogItemDto {
  targetState: BacklogItemState;
}

export interface PauseBacklogItemDto {
  reason?: string;
}

// Task DTOs
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

export interface PauseTaskDto {
  reason?: string;
}

// Event types for SSE
export type ServerEventType =
  | 'backlogItem:created'
  | 'backlogItem:updated'
  | 'backlogItem:deleted'
  | 'backlogItem:log'
  | 'backlogItem:paused'
  | 'backlogItem:resumed'
  | 'task:created'
  | 'task:updated'
  | 'task:deleted'
  | 'task:log'
  | 'task:paused'
  | 'task:resumed'
  | 'agent:statusChanged'
  | 'scheduler:taskScheduled'
  | 'notification:new'
  | 'artifact:created'
  | 'humanGate:requested'
  | 'humanGate:resolved'
  | 'agentQuestion:requested'
  | 'agentQuestion:answered'
  | 'agentQuestion:timeout'
  | 'agentQuestion:cancelled'
  | 'repository:created'
  | 'repository:updated'
  | 'repository:deleted';

// SSE Event Payloads
export interface ServerEvent {
  type: ServerEventType;
  payload: unknown;
  timestamp: Date;
}

// Agent Status
export interface AgentStatus {
  isRunning: boolean;
  currentTaskId?: string;
  currentBacklogItemId?: string;
  startedAt?: Date;
}

// Scheduler Status
export interface SchedulerStatus {
  isEnabled: boolean;
  isAgentRunning: boolean;
  currentTaskId?: string;
  currentBacklogItemId?: string;
  pendingTaskCount: number;
  pausedTaskCount: number;
}

// Repository Interface (full entity)
export interface Repository {
  id: string;
  name: string;
  path: string;
  isActive: boolean;
  branch?: string;
  commitHash?: string;
  remoteUrl?: string;
  isDirty?: boolean;
  isGitRepository: boolean;
  lastRefreshedAt?: Date;
  createdAt: Date;
  updatedAt: Date;
  taskCount: number;
}

// Repository DTOs
export interface CreateRepositoryDto {
  name: string;
  path: string;
}

export interface UpdateRepositoryDto {
  name?: string;
}

// Legacy alias for backward compatibility
export type RepositoryInfo = Repository;

// Human Gate Types
export const HUMAN_GATE_TYPES = ['refining', 'split', 'planning'] as const;
export type HumanGateType = (typeof HUMAN_GATE_TYPES)[number];

// Human Gate Status
export const HUMAN_GATE_STATUSES = ['pending', 'approved', 'rejected', 'skipped'] as const;
export type HumanGateStatus = (typeof HUMAN_GATE_STATUSES)[number];

// Human Gate Interface
export interface HumanGate {
  id: string;
  taskId?: string;
  backlogItemId?: string;
  gateType: HumanGateType;
  status: HumanGateStatus;
  confidenceScore: number;
  reason: string;
  requestedAt: Date;
  resolvedAt?: Date;
  resolvedBy?: string;
  resolution?: string;
}

// DTO for resolving a human gate
export interface ResolveGateDto {
  status: HumanGateStatus;
  resolution?: string;
  resolvedBy?: string;
}

// Agent Question Status
export const AGENT_QUESTION_STATUSES = ['pending', 'answered', 'timeout', 'cancelled'] as const;
export type AgentQuestionStatus = (typeof AGENT_QUESTION_STATUSES)[number];

// Agent Question Option
export interface QuestionOption {
  label: string;
  description: string;
}

// Agent Question Item (one question in a set)
export interface AgentQuestionItem {
  question: string;
  header: string;
  options: QuestionOption[];
  multiSelect: boolean;
}

// User's answer for a single question
export interface QuestionAnswer {
  questionIndex: number;
  selectedOptionIndices: number[];
  customAnswer?: string;
}

// Agent Question Interface
export interface AgentQuestion {
  id: string;
  taskId?: string;
  backlogItemId?: string;
  toolUseId: string;
  questions: AgentQuestionItem[];
  status: AgentQuestionStatus;
  requestedAt: Date;
  timeoutAt: Date;
  answers?: QuestionAnswer[];
  answeredAt?: Date;
}

// DTO for submitting an answer to an agent question
export interface SubmitAnswerDto {
  answers: QuestionAnswer[];
}

// Discriminated union for pending input items
export type PendingInputItem =
  | { type: 'gate'; data: HumanGate }
  | { type: 'question'; data: AgentQuestion };
