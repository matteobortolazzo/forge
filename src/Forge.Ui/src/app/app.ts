import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { RepositorySidebarComponent } from './shared/components/repository-sidebar/repository-sidebar.component';
import { AddRepositoryDialogComponent } from './shared/components/add-repository-dialog/add-repository-dialog.component';
import { RepositorySettingsDialogComponent } from './shared/components/repository-settings-dialog/repository-settings-dialog.component';
import { RepositoryStore } from './core/stores/repository.store';
import { CreateRepositoryDto, Repository } from './shared/models';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RepositorySidebarComponent,
    AddRepositoryDialogComponent,
    RepositorySettingsDialogComponent,
  ],
  template: `
    <div class="flex h-screen">
      <!-- Repository Sidebar -->
      <app-repository-sidebar
        (addRepository)="openAddDialog()"
        (openSettings)="openSettingsDialog($event)"
      />

      <!-- Main Content -->
      <main class="flex-1 overflow-hidden">
        <router-outlet />
      </main>
    </div>

    <!-- Add Repository Dialog -->
    <app-add-repository-dialog
      #addDialog
      [isOpen]="isAddDialogOpen()"
      (create)="onCreateRepository($event)"
      (cancel)="closeAddDialog()"
    />

    <!-- Repository Settings Dialog -->
    <app-repository-settings-dialog
      #settingsDialog
      [isOpen]="isSettingsDialogOpen()"
      [repository]="settingsRepository()"
      (close)="closeSettingsDialog()"
      (delete)="onDeleteRepository($event)"
      (refresh)="onRefreshRepository($event)"
    />
  `,
  styles: `
    :host {
      display: block;
      height: 100vh;
    }
  `,
})
export class App implements OnInit {
  private readonly repositoryStore = inject(RepositoryStore);

  @ViewChild('addDialog') addDialogRef!: AddRepositoryDialogComponent;
  @ViewChild('settingsDialog') settingsDialogRef!: RepositorySettingsDialogComponent;

  readonly isAddDialogOpen = signal(false);
  readonly isSettingsDialogOpen = signal(false);
  readonly settingsRepository = signal<Repository | null>(null);

  ngOnInit(): void {
    // Load repositories on app start
    this.repositoryStore.loadRepositories();
  }

  openAddDialog(): void {
    this.isAddDialogOpen.set(true);
  }

  closeAddDialog(): void {
    this.isAddDialogOpen.set(false);
    this.addDialogRef?.resetForm();
  }

  openSettingsDialog(repository: Repository): void {
    this.settingsRepository.set(repository);
    this.isSettingsDialogOpen.set(true);
  }

  closeSettingsDialog(): void {
    this.isSettingsDialogOpen.set(false);
    this.settingsRepository.set(null);
    this.settingsDialogRef?.resetState();
  }

  async onCreateRepository(dto: CreateRepositoryDto): Promise<void> {
    const repo = await this.repositoryStore.createRepository(dto);
    if (repo) {
      this.closeAddDialog();
    } else {
      this.addDialogRef?.setError(this.repositoryStore.error() ?? 'Failed to create repository');
    }
  }

  async onDeleteRepository(id: string): Promise<void> {
    const success = await this.repositoryStore.deleteRepository(id);
    if (success) {
      this.closeSettingsDialog();
    }
  }

  async onRefreshRepository(id: string): Promise<void> {
    const updated = await this.repositoryStore.refreshRepository(id);
    if (updated) {
      this.settingsRepository.set(updated);
    }
    this.settingsDialogRef?.resetState();
  }
}
