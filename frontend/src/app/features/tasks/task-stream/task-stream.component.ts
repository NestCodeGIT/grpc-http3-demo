import { Component, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatBadgeModule } from '@angular/material/badge';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { GrpcTaskService } from '../../../core/grpc/task.service';

@Component({
  selector: 'app-task-stream',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatBadgeModule, MatSelectModule, MatFormFieldModule,
  ],
  template: `
    <div class="container">
      <div class="header">
        <div>
          <button mat-icon-button routerLink="/">
            <mat-icon>arrow_back</mat-icon>
          </button>
          <h1>Live Task Stream</h1>
        </div>
        <div class="controls">
          <mat-form-field appearance="outline" class="filter-field">
            <mat-label>Filter Status</mat-label>
            <mat-select [(value)]="filterStatus">
              <mat-option value="">All</mat-option>
              <mat-option value="pending">Pending</mat-option>
              <mat-option value="in_progress">In Progress</mat-option>
              <mat-option value="done">Done</mat-option>
            </mat-select>
          </mat-form-field>

          @if (!taskService.isStreaming()) {
            <button mat-raised-button color="accent" (click)="startStream()">
              <mat-icon>play_arrow</mat-icon> Start Stream
            </button>
          } @else {
            <button mat-raised-button color="warn" (click)="stopStream()">
              <mat-icon>stop</mat-icon> Stop Stream
            </button>
          }
        </div>
      </div>

      <!-- Stream info banner -->
      <div class="info-banner">
        <mat-icon>info</mat-icon>
        <span>
          This page uses <strong>gRPC server-streaming</strong> — the server pushes task updates
          every 2 seconds over a single long-lived gRPC connection.
          Transport: <strong>HTTP/3 / QUIC</strong>.
        </span>
      </div>

      <!-- Live indicator -->
      @if (taskService.isStreaming()) {
        <div class="live-indicator">
          <span class="pulse"></span>
          <strong>LIVE</strong> — receiving task updates via gRPC streaming
        </div>
      }

      <!-- Task feed -->
      <div class="stream-feed">
        @for (task of taskService.streamedTasks(); track task.id) {
          <div class="stream-item" [@fadeIn]>
            <div class="task-header">
              <strong>{{ task.title }}</strong>
              <span [class]="'status-chip ' + task.status">{{ task.status }}</span>
            </div>
            @if (task.description) {
              <p class="task-desc">{{ task.description }}</p>
            }
            <div class="task-meta">
              <span>Updated: {{ task.updatedAt | date:'HH:mm:ss' }}</span>
              <code class="task-id">{{ task.id | slice:0:8 }}...</code>
            </div>
          </div>
        }

        @if (taskService.streamedTasks().length === 0) {
          <div class="empty-state">
            <mat-icon>wifi_tethering_off</mat-icon>
            <p>Press <strong>Start Stream</strong> to begin receiving live task updates.</p>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .container { max-width: 800px; margin: 0 auto; padding: 24px; }
    .header { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 16px; margin-bottom: 16px; }
    .header div:first-child { display: flex; align-items: center; gap: 8px; }
    .header h1 { margin: 0; }
    .controls { display: flex; align-items: center; gap: 12px; }
    .filter-field { width: 160px; }
    .info-banner { display: flex; align-items: flex-start; gap: 8px; background: #e8f4fd; border-left: 4px solid #1976d2; padding: 12px 16px; border-radius: 4px; margin-bottom: 16px; font-size: 14px; }
    .live-indicator { display: flex; align-items: center; gap: 8px; padding: 8px 16px; background: #e8f5e9; border-radius: 20px; width: fit-content; margin-bottom: 16px; color: #2e7d32; }
    .pulse { width: 10px; height: 10px; background: #4caf50; border-radius: 50%; animation: pulse 1s infinite; }
    @keyframes pulse { 0%, 100% { opacity: 1; transform: scale(1); } 50% { opacity: 0.5; transform: scale(1.4); } }
    .stream-feed { display: flex; flex-direction: column; gap: 8px; }
    .stream-item { padding: 12px 16px; border: 1px solid #e0e0e0; border-radius: 8px; background: #fff; transition: background 0.3s; }
    .stream-item:hover { background: #f9f9f9; }
    .task-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px; }
    .task-desc { margin: 4px 0; color: #555; font-size: 14px; }
    .task-meta { display: flex; justify-content: space-between; font-size: 12px; color: #999; margin-top: 6px; }
    .task-id { background: #f5f5f5; padding: 1px 6px; border-radius: 4px; }
    .status-chip { padding: 2px 8px; border-radius: 8px; font-size: 11px; font-weight: 600; }
    .status-chip.pending { background: #fff3e0; color: #e65100; }
    .status-chip.in_progress { background: #e3f2fd; color: #1565c0; }
    .status-chip.done { background: #e8f5e9; color: #2e7d32; }
    .status-chip.cancelled { background: #fce4ec; color: #880e4f; }
    .empty-state { text-align: center; padding: 64px; color: #bbb; }
    .empty-state mat-icon { font-size: 64px; height: 64px; width: 64px; display: block; margin: 0 auto 16px; }
  `]
})
export class TaskStreamComponent implements OnDestroy {
  taskService = inject(GrpcTaskService);
  filterStatus = '';

  startStream(): void {
    this.taskService.startStream(this.filterStatus);
  }

  stopStream(): void {
    this.taskService.stopStream();
  }

  ngOnDestroy(): void {
    this.taskService.stopStream();
  }
}
