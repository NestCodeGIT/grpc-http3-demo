import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { GrpcTaskService, Task } from '../../core/grpc/task.service';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatInputModule, MatFormFieldModule, MatChipsModule,
    MatProgressSpinnerModule, MatSnackBarModule, MatDialogModule,
  ],
  template: `
    <div class="container">
      <div class="header">
        <h1>Task Manager</h1>
        <div class="protocol-badge">
          <span class="badge http3">HTTP/3 + QUIC</span>
          <span class="badge grpc">gRPC-Web</span>
        </div>
      </div>

      <!-- Create Task Form -->
      <mat-card class="create-card">
        <mat-card-header>
          <mat-card-title>New Task</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="createTask()">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Title</mat-label>
              <input matInput formControlName="title" placeholder="Task title...">
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Description</mat-label>
              <textarea matInput formControlName="description" rows="2"></textarea>
            </mat-form-field>
            <button mat-raised-button color="primary" type="submit"
                    [disabled]="form.invalid || loading()">
              <mat-icon>add</mat-icon> Create Task
            </button>
          </form>
        </mat-card-content>
      </mat-card>

      <!-- Task List -->
      <div class="tasks-section">
        <div class="section-header">
          <h2>Tasks ({{ tasks().length }})</h2>
          <div>
            <a mat-stroked-button routerLink="/stream">
              <mat-icon>stream</mat-icon> Live Stream
            </a>
            <button mat-icon-button (click)="loadTasks()" title="Refresh">
              <mat-icon>refresh</mat-icon>
            </button>
          </div>
        </div>

        @if (loading()) {
          <div class="center"><mat-spinner diameter="48"></mat-spinner></div>
        }

        @for (task of tasks(); track task.id) {
          <mat-card class="task-card">
            <mat-card-header>
              <mat-card-title>{{ task.title }}</mat-card-title>
              <mat-card-subtitle>
                <span [class]="'status-chip ' + task.status">{{ task.status }}</span>
                <span class="date">{{ task.createdAt | date:'short' }}</span>
              </mat-card-subtitle>
            </mat-card-header>
            @if (task.description) {
              <mat-card-content>
                <p>{{ task.description }}</p>
              </mat-card-content>
            }
            <mat-card-actions>
              <button mat-button color="primary" (click)="markDone(task)"
                      [disabled]="task.status === 'done'">
                <mat-icon>check</mat-icon> Done
              </button>
              <button mat-button color="warn" (click)="deleteTask(task.id)">
                <mat-icon>delete</mat-icon>
              </button>
            </mat-card-actions>
          </mat-card>
        }

        @if (!loading() && tasks().length === 0) {
          <div class="empty-state">
            <mat-icon>task_alt</mat-icon>
            <p>No tasks yet. Create one above.</p>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .container { max-width: 800px; margin: 0 auto; padding: 24px; }
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
    .protocol-badge { display: flex; gap: 8px; }
    .badge { padding: 4px 12px; border-radius: 16px; font-size: 12px; font-weight: 600; }
    .http3 { background: #e8f5e9; color: #2e7d32; }
    .grpc { background: #e3f2fd; color: #1565c0; }
    .create-card { margin-bottom: 24px; }
    .full-width { width: 100%; margin-bottom: 8px; }
    .tasks-section { display: flex; flex-direction: column; gap: 12px; }
    .section-header { display: flex; justify-content: space-between; align-items: center; }
    .task-card { transition: box-shadow 0.2s; }
    .task-card:hover { box-shadow: 0 4px 16px rgba(0,0,0,0.15); }
    .status-chip { padding: 2px 8px; border-radius: 8px; font-size: 11px; font-weight: 600; }
    .status-chip.pending { background: #fff3e0; color: #e65100; }
    .status-chip.in_progress { background: #e3f2fd; color: #1565c0; }
    .status-chip.done { background: #e8f5e9; color: #2e7d32; }
    .status-chip.cancelled { background: #fce4ec; color: #880e4f; }
    .date { margin-left: 8px; font-size: 12px; color: #666; }
    .center { display: flex; justify-content: center; padding: 48px; }
    .empty-state { text-align: center; padding: 48px; color: #999; }
    .empty-state mat-icon { font-size: 48px; height: 48px; width: 48px; }
  `]
})
export class TaskListComponent implements OnInit {
  private taskService = inject(GrpcTaskService);
  private snack = inject(MatSnackBar);
  private fb = inject(FormBuilder);

  tasks = signal<Task[]>([]);
  loading = signal(false);

  form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
  });

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading.set(true);
    this.taskService.listTasks().subscribe({
      next: tasks => { this.tasks.set(tasks); this.loading.set(false); },
      error: err => { this.snack.open('Failed to load tasks: ' + err.message, 'Close', { duration: 4000 }); this.loading.set(false); }
    });
  }

  createTask(): void {
    if (this.form.invalid) return;
    const { title, description } = this.form.value;
    this.loading.set(true);
    this.taskService.createTask(title!, description ?? '').subscribe({
      next: task => {
        this.tasks.update(ts => [task, ...ts]);
        this.form.reset();
        this.loading.set(false);
        this.snack.open('Task created!', '', { duration: 2000 });
      },
      error: err => { this.snack.open('Error: ' + err.message, 'Close', { duration: 4000 }); this.loading.set(false); }
    });
  }

  markDone(task: Task): void {
    this.taskService.updateTask(task.id, task.title, 'done').subscribe({
      next: updated => this.tasks.update(ts => ts.map(t => t.id === updated.id ? updated : t)),
      error: err => this.snack.open('Error: ' + err.message, 'Close', { duration: 4000 })
    });
  }

  deleteTask(id: string): void {
    this.taskService.deleteTask(id).subscribe({
      next: () => { this.tasks.update(ts => ts.filter(t => t.id !== id)); this.snack.open('Deleted', '', { duration: 1500 }); },
      error: err => this.snack.open('Error: ' + err.message, 'Close', { duration: 4000 })
    });
  }
}
