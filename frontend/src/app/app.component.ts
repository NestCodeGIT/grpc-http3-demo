import { Component } from '@angular/core';
import { RouterOutlet, RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterModule, MatToolbarModule, MatButtonModule, MatIconModule],
  template: `
    <mat-toolbar color="primary">
      <mat-icon style="margin-right:8px">bolt</mat-icon>
      <span>gRPC + HTTP/3 Demo</span>
      <span style="flex:1"></span>
      <a mat-button routerLink="/">Tasks</a>
      <a mat-button routerLink="/stream">Live Stream</a>
      <a mat-button href="https://github.com/yourusername/grpc-http3-demo" target="_blank">
        <mat-icon>code</mat-icon> GitHub
      </a>
    </mat-toolbar>
    <router-outlet />
  `,
})
export class AppComponent {}
